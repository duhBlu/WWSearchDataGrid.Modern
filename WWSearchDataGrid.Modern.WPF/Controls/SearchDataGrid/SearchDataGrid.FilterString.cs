using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using WWSearchDataGrid.Modern.Core;
using WWSearchDataGrid.Modern.WPF.Display;

namespace WWSearchDataGrid.Modern.WPF
{
    public partial class SearchDataGrid
    {
        #region FilterString DP

        /// <summary>
        /// Gets or sets a DevExpress-style criteria expression that pre-seeds filters from XAML.
        /// </summary>
        /// <remarks>
        /// Grammar: bracketed field names (<c>[Field]</c>), <c>And</c> / <c>Or</c> keywords,
        /// parenthesised grouping, comparison operators (<c>=</c>, <c>&lt;&gt;</c>, <c>&lt;</c>,
        /// <c>&lt;=</c>, <c>&gt;</c>, <c>&gt;=</c>), and function-call predicates such as
        /// <c>IsNull([F])</c>, <c>Contains([F], 'x')</c>, <c>IsOutlookIntervalToday([F])</c>.
        /// Operator precedence: <c>And</c> binds tighter than <c>Or</c>. Use parentheses to
        /// override.
        ///
        /// Setting a new value resets ALL filter state on every column referenced by either
        /// the old or new expression — including user-added filters on those columns. Columns
        /// not mentioned by either expression keep their filters. Set to <c>null</c> or empty
        /// to clear all seeded filters.
        ///
        /// In XAML, escape <c>&lt;</c> and <c>&gt;</c> as <c>&amp;lt;</c> and <c>&amp;gt;</c>.
        /// </remarks>
        public static readonly DependencyProperty FilterStringProperty =
            DependencyProperty.Register(
                nameof(FilterString),
                typeof(string),
                typeof(SearchDataGrid),
                new PropertyMetadata(null, OnFilterStringChanged));

        public string FilterString
        {
            get => (string)GetValue(FilterStringProperty);
            set => SetValue(FilterStringProperty, value);
        }

        private static void OnFilterStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                grid._cachedFilterStringParse = null;
                grid.ApplyFilterString();
            }
        }

        #endregion

        #region Apply pipeline

        private bool _isApplyingFilterString;
        private FilterStringParseResult _cachedFilterStringParse;
        private FilterStringParseResult _previousAppliedFilterStringParse;

        /// <summary>
        /// Applies the current <see cref="FilterString"/> to the grid. Safe to call from any
        /// lifecycle hook: bails out cleanly if the grid isn't ready (no columns or no
        /// <see cref="ItemsControl.ItemsSource"/>) and is re-tried by later triggers.
        /// </summary>
        internal void ApplyFilterString()
        {
            if (_isApplyingFilterString) return;
            _isApplyingFilterString = true;
            try
            {
                var raw = FilterString;
                if (string.IsNullOrWhiteSpace(raw))
                {
                    if (_previousAppliedFilterStringParse != null)
                    {
                        ClearFilterStringSeededState(_previousAppliedFilterStringParse, FilterStringParseResult.Empty);
                        _previousAppliedFilterStringParse = null;
                        FilterItemsSource();
                    }
                    _cachedFilterStringParse = null;
                    return;
                }

                var descriptors = (FreezableCollection<GridColumn>)GetValue(GridColumnsProperty);
                if (descriptors == null || descriptors.Count == 0) return;
                if (ItemsSource == null) return;

                var parse = _cachedFilterStringParse ?? FilterStringParser.Parse(raw);
                _cachedFilterStringParse = parse;
                LogDiagnostics(parse);
                if (parse.IsFatal)
                {
                    if (_previousAppliedFilterStringParse != null)
                    {
                        ClearFilterStringSeededState(_previousAppliedFilterStringParse, FilterStringParseResult.Empty);
                        _previousAppliedFilterStringParse = null;
                        FilterItemsSource();
                    }
                    return;
                }

                ClearFilterStringSeededState(_previousAppliedFilterStringParse, parse);
                BindClausesToControllers(parse, descriptors);
                _previousAppliedFilterStringParse = parse;
                FilterItemsSource();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FilterString apply failed: {ex.Message}");
            }
            finally
            {
                _isApplyingFilterString = false;
            }
        }

        private static void LogDiagnostics(FilterStringParseResult parse)
        {
            if (parse?.Diagnostics == null) return;
            for (int i = 0; i < parse.Diagnostics.Count; i++)
                Debug.WriteLine("FilterString: " + parse.Diagnostics[i]);
        }

        private void ClearFilterStringSeededState(FilterStringParseResult oldParse, FilterStringParseResult newParse)
        {
            var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (oldParse?.Clauses != null)
                foreach (var c in oldParse.Clauses) if (!string.IsNullOrEmpty(c.FieldName)) fieldNames.Add(c.FieldName);
            if (newParse?.Clauses != null)
                foreach (var c in newParse.Clauses) if (!string.IsNullOrEmpty(c.FieldName)) fieldNames.Add(c.FieldName);
            if (fieldNames.Count == 0) return;

            var descriptors = (FreezableCollection<GridColumn>)GetValue(GridColumnsProperty);
            if (descriptors == null) return;

            foreach (var descriptor in descriptors)
            {
                if (descriptor?.SearchTemplateController == null) continue;
                if (!fieldNames.Contains(descriptor.FieldName) && !fieldNames.Contains(descriptor.FilterMemberPath ?? string.Empty))
                    continue;
                descriptor.SearchTemplateController.SearchGroups.Clear();
                descriptor.SearchTemplateController.UpdateFilterExpression();
            }
        }

        private void BindClausesToControllers(FilterStringParseResult parse, FreezableCollection<GridColumn> descriptors)
        {
            if (parse?.Clauses == null || parse.Clauses.Count == 0) return;

            var touched = new HashSet<SearchTemplateController>();
            var groupHandles = new Dictionary<(SearchTemplateController, int), SearchTemplateGroup>();
            var groupHasFirstTemplate = new Dictionary<(SearchTemplateController, int), bool>();

            for (int i = 0; i < parse.Clauses.Count; i++)
            {
                var clause = parse.Clauses[i];
                var descriptor = FindDescriptorByFieldName(descriptors, clause.FieldName);
                if (descriptor == null)
                {
                    Debug.WriteLine($"FilterString: no column descriptor for field '{clause.FieldName}'; skipping clause.");
                    continue;
                }

                var controller = EnsureControllerBootstrapped(descriptor);
                if (controller == null) continue;

                if (touched.Add(controller))
                {
                    // First clause for this controller — start with a clean slate so we own its
                    // SearchGroups entirely. SetupColumnDataLazy added a starter group during
                    // bootstrap; drop it.
                    controller.SearchGroups.Clear();
                }

                var key = (controller, clause.GroupIndex);
                if (!groupHandles.TryGetValue(key, out var group))
                {
                    // Route through AddSearchGroup so the new group's template is properly
                    // subscribed to the controller's PropertyChanged handler — otherwise
                    // post-seed UI edits would silently fail to re-evaluate the filter.
                    controller.AddSearchGroup(canAddGroup: true, markAsChanged: false);
                    group = controller.SearchGroups[controller.SearchGroups.Count - 1];
                    groupHandles[key] = group;
                    groupHasFirstTemplate[key] = false;
                    if (!string.IsNullOrEmpty(clause.Combinator))
                        group.OperatorName = clause.Combinator;
                }

                if (!TryValidateSearchTypeForColumn(clause.SearchType, controller.ColumnDataType, descriptor, out var validationError))
                {
                    Debug.WriteLine($"FilterString: {validationError}");
                    continue;
                }

                SearchTemplate template;
                if (!groupHasFirstTemplate[key])
                {
                    // AddSearchGroup created one blank template in this group; reuse it.
                    template = group.SearchTemplates[0];
                    groupHasFirstTemplate[key] = true;
                }
                else
                {
                    controller.AddSearchTemplate(markAsChanged: false, referenceTemplate: null, group: group);
                    template = group.SearchTemplates[group.SearchTemplates.Count - 1];
                    if (!string.IsNullOrEmpty(clause.Combinator))
                        template.OperatorName = clause.Combinator;
                }

                template.SearchType = clause.SearchType;
                if (!ApplyClauseValues(clause, descriptor, template))
                    continue;
            }

            foreach (var controller in touched)
                controller.UpdateFilterExpression();
        }

        private static GridColumn FindDescriptorByFieldName(FreezableCollection<GridColumn> descriptors, string fieldName)
        {
            if (descriptors == null || string.IsNullOrEmpty(fieldName)) return null;
            foreach (var d in descriptors)
            {
                if (d == null) continue;
                if (string.Equals(d.FieldName, fieldName, StringComparison.OrdinalIgnoreCase)) return d;
                if (!string.IsNullOrEmpty(d.FilterMemberPath)
                    && string.Equals(d.FilterMemberPath, fieldName, StringComparison.OrdinalIgnoreCase))
                    return d;
            }
            return null;
        }

        private static bool TryValidateSearchTypeForColumn(SearchType type, ColumnDataType dataType, GridColumn descriptor, out string error)
        {
            error = null;
            switch (type)
            {
                case SearchType.Contains:
                case SearchType.DoesNotContain:
                case SearchType.StartsWith:
                case SearchType.EndsWith:
                case SearchType.IsLike:
                case SearchType.IsNotLike:
                    if (dataType != ColumnDataType.String && dataType != ColumnDataType.Enum && dataType != ColumnDataType.Unknown)
                    {
                        error = $"SearchType {type} is not valid for column '{descriptor.FieldName}' (data type {dataType}); skipping.";
                        return false;
                    }
                    return true;
                case SearchType.Today:
                case SearchType.Yesterday:
                case SearchType.DateInterval:
                case SearchType.BetweenDates:
                case SearchType.NotBetweenDates:
                case SearchType.IsOnAnyOfDates:
                    if (dataType != ColumnDataType.DateTime && dataType != ColumnDataType.Unknown)
                    {
                        error = $"SearchType {type} is not valid for column '{descriptor.FieldName}' (data type {dataType}); skipping.";
                        return false;
                    }
                    return true;
                default:
                    return true;
            }
        }

        private static bool ApplyClauseValues(FilterStringClause clause, GridColumn descriptor, SearchTemplate template)
        {
            var targetType = ResolveCoercionType(descriptor, clause);
            switch (clause.SearchType)
            {
                case SearchType.IsNull:
                case SearchType.IsNotNull:
                case SearchType.Today:
                case SearchType.Yesterday:
                case SearchType.AboveAverage:
                case SearchType.BelowAverage:
                case SearchType.Unique:
                case SearchType.Duplicate:
                    return true;
                case SearchType.Between:
                case SearchType.NotBetween:
                case SearchType.BetweenDates:
                case SearchType.NotBetweenDates:
                    if (!TryCoerce(clause.RawPrimary, targetType, out var p1)) return Reject(clause, "primary");
                    if (!TryCoerce(clause.RawSecondary, targetType, out var p2)) return Reject(clause, "secondary");
                    template.SelectedValue = p1;
                    template.SelectedSecondaryValue = p2;
                    return true;
                case SearchType.IsAnyOf:
                case SearchType.IsNoneOf:
                    if (clause.RawValues == null) return false;
                    foreach (var rv in clause.RawValues)
                    {
                        if (!TryCoerce(rv, targetType, out var v)) return Reject(clause, "list");
                        template.SelectedValues.Add(new SelectableValueItem(v));
                    }
                    return true;
                case SearchType.DateInterval:
                    if (clause.DateInterval.HasValue)
                    {
                        foreach (var item in template.DateIntervals)
                        {
                            if (item.Interval == clause.DateInterval.Value)
                            {
                                item.IsSelected = true;
                                return true;
                            }
                        }
                    }
                    return false;
                case SearchType.IsOnAnyOfDates:
                    if (clause.RawValues == null) return false;
                    foreach (var rv in clause.RawValues)
                    {
                        var dt = TypeTranslatorHelper.ConvertToDateTime(rv);
                        if (!dt.HasValue) return Reject(clause, "date list");
                        template.SelectedDates.Add(dt.Value);
                    }
                    return true;
                default:
                    if (!TryCoerce(clause.RawPrimary, targetType, out var pv)) return Reject(clause, "primary");
                    template.SelectedValue = pv;
                    return true;
            }
        }

        private static bool Reject(FilterStringClause clause, string slot)
        {
            Debug.WriteLine($"FilterString: cannot coerce {slot} value '{clause.RawPrimary ?? clause.RawSecondary}' for field '{clause.FieldName}'; skipping clause.");
            return false;
        }

        private static Type ResolveCoercionType(GridColumn descriptor, FilterStringClause clause)
        {
            var ft = descriptor?.FieldType;
            if (ft != null) return Nullable.GetUnderlyingType(ft) ?? ft;
            return typeof(string);
        }

        private static bool TryCoerce(string raw, Type targetType, out object value)
        {
            value = null;
            if (raw == null) { value = null; return true; }
            if (targetType == typeof(string)) { value = raw; return true; }
            if (targetType == typeof(DateTime))
            {
                var dt = TypeTranslatorHelper.ConvertToDateTime(raw);
                if (dt.HasValue) { value = dt.Value; return true; }
                return false;
            }
            if (targetType == typeof(bool))
            {
                if (bool.TryParse(raw, out var b)) { value = b; return true; }
                return false;
            }
            if (targetType.IsEnum)
            {
                try { value = Enum.Parse(targetType, raw, ignoreCase: true); return true; }
                catch { return false; }
            }
            if (ReflectionHelper.IsNumericType(targetType))
            {
                var dec = TypeTranslatorHelper.ConvertToDecimal(raw);
                if (dec.HasValue)
                {
                    try { value = Convert.ChangeType(dec.Value, targetType, CultureInfo.InvariantCulture); return true; }
                    catch { return false; }
                }
                return false;
            }
            // Fallback: try IConvertible
            try { value = Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture); return true; }
            catch { return false; }
        }

        #endregion

        #region Shared controller bootstrap

        /// <summary>
        /// Ensures the descriptor has a <see cref="SearchTemplateController"/> initialised with
        /// its column name, values provider, data type, and display helpers. Idempotent —
        /// repeat calls only run once.
        /// </summary>
        /// <remarks>
        /// Called by the <see cref="FilterString"/> apply path and by the auto-filter-row UI in
        /// <see cref="ColumnFilterControl"/>. Centralised so the two callers can't drift.
        /// </remarks>
        internal SearchTemplateController EnsureControllerBootstrapped(GridColumn descriptor)
        {
            if (descriptor == null) return null;

            if (descriptor.SearchTemplateController == null)
                descriptor.SearchTemplateController = new SearchTemplateController();

            var controller = descriptor.SearchTemplateController;

            var bindingPath = !string.IsNullOrEmpty(descriptor.FilterMemberPath)
                ? descriptor.FilterMemberPath
                : descriptor.FieldName;

            if (controller.ColumnName == null)
            {
                var displayName = !string.IsNullOrEmpty(descriptor.ColumnDisplayName)
                    ? descriptor.ColumnDisplayName
                    : descriptor.HeaderCaption;
                controller.SetupColumnDataLazy(
                    displayName,
                    BuildColumnValuesProvider(descriptor, bindingPath),
                    bindingPath);
            }

            // Resolve ColumnDataType from FieldType straight away so callers don't have to wait
            // for value-sampling. ResolveFieldTypesFromItemsSource always runs before this on
            // the FilterString path.
            var dataType = MapFieldTypeToColumnDataType(descriptor.FieldType);
            if (dataType != ColumnDataType.Unknown && controller.ColumnDataType != dataType)
                controller.ColumnDataType = dataType;

            if (controller.DisplayValueProvider == null)
                controller.DisplayValueProvider = DisplayValueProviderFactory.Create(descriptor);
            if (string.IsNullOrEmpty(controller.DisplayMaskPattern))
                controller.DisplayMaskPattern = descriptor.DisplayMask;

            if (controller.ColumnDataType == ColumnDataType.DateTime)
                controller.RoundDateTime = descriptor.ResolveEffectiveRoundDateTime();

            return controller;
        }

        internal static Func<IEnumerable<object>> BuildColumnValuesProvider(GridColumn descriptor, string bindingPath)
        {
            return () =>
            {
                if (descriptor?.Owner == null || string.IsNullOrEmpty(bindingPath))
                    return Enumerable.Empty<object>();
                IEnumerable source = descriptor.Owner.OriginalItemsSource ?? descriptor.Owner.Items;
                if (source == null) return Enumerable.Empty<object>();
                var values = new List<object>();
                foreach (var item in source)
                    values.Add(ReflectionHelper.GetPropValue(item, bindingPath));
                return values;
            };
        }

        internal static ColumnDataType MapFieldTypeToColumnDataType(Type fieldType)
        {
            if (fieldType == null) return ColumnDataType.Unknown;
            var t = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
            if (t == typeof(string)) return ColumnDataType.String;
            if (t == typeof(DateTime)) return ColumnDataType.DateTime;
            if (t == typeof(bool)) return ColumnDataType.Boolean;
            if (t.IsEnum) return ColumnDataType.Enum;
            if (ReflectionHelper.IsNumericType(t)) return ColumnDataType.Number;
            return ColumnDataType.String;
        }

        #endregion
    }
}
