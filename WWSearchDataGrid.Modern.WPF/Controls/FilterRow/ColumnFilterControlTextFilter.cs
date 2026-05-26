using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Text + typed-value filtering for <see cref="ColumnFilterControl"/>. The active
    /// <see cref="SearchType"/> comes from the visible <see cref="SearchTypeSelector"/>.
    /// </summary>
    public partial class ColumnFilterControl
    {
        #region DP change

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnFilterControl ctl) return;
            if (ctl._filterPopup?.IsOpen == true) return;
            // Programmatic resets (NoInput, clear) manage the temp template themselves.
            if (ctl._suppressSearchTextSync) return;

            if (string.IsNullOrWhiteSpace((string)e.NewValue))
            {
                ctl.ClearTemporaryTemplate();
                return;
            }

            if (!ctl.EffectiveIsLiveFilteringEnabled)
                return;

            // Materialize the template up front so filter-state flags track the typed text
            // immediately; only the filter rebuild is debounced.
            ctl.CreateTemporaryTemplateImmediate();

            int delay = ctl.SourceDataGrid?.FilterRowDelay ?? 0;
            if (delay <= 0)
            {
                ctl.UpdateSimpleFilter();
                return;
            }

            ctl.EnsureChangeTimer();
            ctl._changeTimer.Interval = TimeSpan.FromMilliseconds(delay);
            ctl._changeTimer.Stop();
            ctl._changeTimer.Start();
        }

        private static void OnSearchValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnFilterControl ctl) return;
            if (ctl._filterPopup?.IsOpen == true) return;
            // Programmatic resets (NoInput, clear) manage the temp template themselves.
            if (ctl._suppressSearchTextSync) return;

            // Typed editors write through this DP — null/empty clears, anything else applies
            // via the active SearchType.
            if (e.NewValue == null
                || (e.NewValue is string s && string.IsNullOrWhiteSpace(s)))
            {
                ctl.ClearTemporaryTemplate();
                return;
            }

            ctl.CreateTemporaryTemplateImmediate();

            // Typed editors don't debounce — DatePicker/ComboBox commit on user action, not
            // keystroke. Debounce is only meaningful for OnSearchTextChanged.
            ctl.UpdateSimpleFilter();
        }

        #endregion

        #region TextBox event handlers

        private void OnSearchTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            // Filter rebuild runs in OnSearchTextChanged; this hook only blocks the typing
            // path while the rule-filter popup owns the controller.
            if (_filterPopup?.IsOpen == true) return;
        }

        private void OnSearchTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ClearSearchTextAndTemporaryFilter();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Enter)
            {
                if (IsComplexFilteringEnabled)
                    CreatePermanentFilterAndRefocus();
                else
                    CommitSearchText();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Tab && !string.IsNullOrWhiteSpace(SearchText))
            {
                // Commit on Tab so leaving doesn't drop an in-flight filter. Not marked Handled
                // — the host's bubble OnKeyDown hands Tab to FilterRowNavigator after.
                CommitSearchText();
                return;
            }

            // Arrow nav: Left/Right step cells only at caret boundary; Down hands off to the
            // data row; Up is not forwarded.
            if ((e.Key == Key.Left || e.Key == Key.Right) && IsCaretAtTextBoundary(sender, e.Key))
            {
                FilterRowNavigator.TryNavigate(this, e);
                return;
            }
            if (e.Key == Key.Down)
            {
                FilterRowNavigator.TryNavigate(this, e);
            }
        }

        /// <summary>
        /// Enter/Escape/Tab/arrow handler for the editor produced by
        /// <see cref="BaseEditSettings.CreateFilterEditor"/> — works across every editor shape.
        /// </summary>
        private void OnFilterEditorPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ClearSearchTextAndTemporaryFilter();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                // NoInput types commit on Enter without typed input — the temp template carries
                // the intent. Reset to the default operator after so the next entry starts fresh.
                bool noInput = !ActiveSearchTypeRequiresInput;
                if (!noInput && !HasAnyInputValue()) return;

                if (IsComplexFilteringEnabled)
                    CreatePermanentFilterAndRefocus();
                else
                    CommitSearchText();

                if (noInput)
                    ResetSelectedSearchTypeToDefault();

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Tab && (HasAnyInputValue() || !ActiveSearchTypeRequiresInput))
            {
                // Commit on Tab so leaving doesn't drop an in-flight filter. Not marked Handled
                // — the host's bubble OnKeyDown hands Tab to FilterRowNavigator after.
                CommitSearchText();
                return;
            }

            // Arrow nav: Left/Right step cells only at caret boundary (false for non-TextBox);
            // Down hands off to the data row; Up is not forwarded.
            if ((e.Key == Key.Left || e.Key == Key.Right) && IsCaretAtTextBoundary(sender, e.Key))
            {
                FilterRowNavigator.TryNavigate(this, e);
                return;
            }
            if (e.Key == Key.Down)
            {
                FilterRowNavigator.TryNavigate(this, e);
            }
        }

        /// <summary>
        /// True for a TextBox with no selection and the caret at the start (Left) or end
        /// (Right). Mid-text caret or active selection keeps the arrow for caret movement.
        /// </summary>
        private static bool IsCaretAtTextBoundary(object sender, Key key)
        {
            if (sender is not TextBox tb) return false;
            if (tb.SelectionLength > 0) return false;
            if (key == Key.Left) return tb.CaretIndex == 0;
            if (key == Key.Right) return tb.CaretIndex == (tb.Text?.Length ?? 0);
            return false;
        }

        #endregion

        #region Template lifecycle

        /// <summary>
        /// Returns <see cref="SearchText"/>, with mask literals stripped when the column has a display mask.
        /// </summary>
        private string GetEffectiveSearchText()
        {
            string text = SearchText;
            if (SearchTemplateController?.DisplayValueProvider is Core.Display.MaskDisplayProvider maskProvider
                && !string.IsNullOrEmpty(text))
            {
                text = maskProvider.StripLiterals(text);
            }
            return text ?? string.Empty;
        }

        /// <summary>Effective filter value — <see cref="SearchValue"/> for typed editors, otherwise <see cref="SearchText"/>.</summary>
        private object GetEffectiveFilterValue()
        {
            if (SearchValue != null && !(SearchValue is string s && string.IsNullOrWhiteSpace(s)))
                return SearchValue;
            return GetEffectiveSearchText();
        }

        private void CreateTemporaryTemplateImmediate()
        {
            try
            {
                if (SearchTemplateController == null) return;
                if (!HasAnyInputValue()) return;

                if (SearchTemplateController.SearchGroups.Count == 0)
                    SearchTemplateController.AddSearchGroup();

                var firstGroup = SearchTemplateController.SearchGroups[0];
                RemoveDefaultEmptyTemplates(firstGroup);

                var searchType = SelectedSearchType;
                var value = GetEffectiveFilterValue();

                if (_temporarySearchTemplate != null)
                {
                    _temporarySearchTemplate.SearchType = searchType;
                    _temporarySearchTemplate.SelectedValue = value;
                }
                else
                {
                    _temporarySearchTemplate = new SearchTemplate(SearchTemplateController.ColumnDataType)
                    {
                        SearchType = searchType,
                        SelectedValue = value,
                        SearchTemplateController = SearchTemplateController,
                    };
                    SearchTemplateController.SubscribeToTemplateChanges(_temporarySearchTemplate);

                    var existingOfSameType = firstGroup.SearchTemplates
                        .Where(t => t.SearchType == searchType && t.HasCustomFilter)
                        .ToList();
                    if (existingOfSameType.Any())
                        _temporarySearchTemplate.OperatorName = "Or";

                    firstGroup.SearchTemplates.Add(_temporarySearchTemplate);
                }

                SearchTemplateController.UpdateOperatorVisibility();
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CreateTemporaryTemplateImmediate: {ex.Message}");
            }
        }

        private bool HasAnyInputValue()
        {
            if (!string.IsNullOrWhiteSpace(SearchText)) return true;
            if (SearchValue == null) return false;
            if (SearchValue is string sv) return !string.IsNullOrWhiteSpace(sv);
            return true;
        }

        private void ClearTemporaryTemplate()
        {
            try
            {
                _changeTimer?.Stop();
                if (_temporarySearchTemplate != null && SearchTemplateController?.SearchGroups?.Count > 0)
                {
                    var firstGroup = SearchTemplateController.SearchGroups[0];
                    firstGroup.SearchTemplates.Remove(_temporarySearchTemplate);
                    _temporarySearchTemplate = null;

                    SearchTemplateController.UpdateFilterExpression();
                    SourceDataGrid?.FilterItemsSource();

                    HasAdvancedFilter = SearchTemplateController?.HasCustomExpression ?? false;
                    UpdateHasActiveFilterState();
                    SourceDataGrid?.UpdateFilterPanel();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ClearTemporaryTemplate: {ex.Message}");
            }
        }

        private void ClearSearchTextOnly()
        {
            try
            {
                _suppressSearchTextSync = true;
                try
                {
                    SearchText = string.Empty;
                    SearchValue = null;
                }
                finally { _suppressSearchTextSync = false; }

                if (_searchTextBox != null)
                    _searchTextBox.Text = string.Empty;

                HasAdvancedFilter = SearchTemplateController?.HasCustomExpression ?? false;
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ClearSearchTextOnly: {ex.Message}");
            }
        }

        private void ClearSearchTextAndTemporaryFilter()
        {
            try
            {
                _changeTimer?.Stop();

                // Capture before clearing — we reset SelectedSearchType at the end only when
                // the cell was on a NoInput type, so the next entry starts on the default operator.
                bool wasNoInput = !ActiveSearchTypeRequiresInput;

                if (IsCheckboxColumn)
                {
                    ResetCheckboxToInitialState();
                }
                else
                {
                    _suppressSearchTextSync = true;
                    try
                    {
                        SearchText = string.Empty;
                        SearchValue = null;
                    }
                    finally { _suppressSearchTextSync = false; }

                    if (_searchTextBox != null)
                        _searchTextBox.Text = string.Empty;

                    if (_temporarySearchTemplate != null && SearchTemplateController?.SearchGroups?.Count > 0)
                    {
                        var firstGroup = SearchTemplateController.SearchGroups[0];
                        firstGroup.SearchTemplates.Remove(_temporarySearchTemplate);
                        _temporarySearchTemplate = null;

                        SearchTemplateController.UpdateFilterExpression();
                        SourceDataGrid?.FilterItemsSource();
                    }

                    HasAdvancedFilter = SearchTemplateController?.HasCustomExpression ?? false;
                }

                UpdateHasActiveFilterState();
                SourceDataGrid?.UpdateFilterPanel();

                if (wasNoInput && !IsCheckboxColumn)
                    ResetSelectedSearchTypeToDefault();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ClearSearchTextAndTemporaryFilter: {ex.Message}");
            }
        }

        private void CommitSearchText()
        {
            try
            {
                if (SearchTemplateController == null || SourceDataGrid == null) return;
                // NoInput types commit without input — the temp template carries the intent.
                if (!HasAnyInputValue() && ActiveSearchTypeRequiresInput) return;

                if (ActiveSearchTypeRequiresInput)
                    CreateTemporaryTemplateImmediate();
                else
                    CreateOrRetargetNoInputTemporaryTemplate();
                SearchTemplateController.UpdateFilterExpression();
                SourceDataGrid.FilterItemsSource();

                HasAdvancedFilter = SearchTemplateController.HasCustomExpression;
                UpdateHasActiveFilterState();
                SourceDataGrid.UpdateFilterPanel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CommitSearchText: {ex.Message}");
            }
        }

        private void CreatePermanentFilterAndRefocus()
        {
            try
            {
                _changeTimer?.Stop();
                if (!HasAnyInputValue() && ActiveSearchTypeRequiresInput) return;

                AddIncrementalFilter();
                ClearSearchTextOnly();

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Prefer the materialized editor; PART_SearchTextBox fallback exists for
                    // any host still exposing the legacy template slot.
                    var focusTarget = (IInputElement)_filterEditor ?? _searchTextBox;
                    if (focusTarget == null) return;
                    focusTarget.Focus();
                    Keyboard.Focus(focusTarget);
                }), DispatcherPriority.Input);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CreatePermanentFilterAndRefocus: {ex.Message}");
            }
        }

        private void AddIncrementalFilter()
        {
            try
            {
                if (SearchTemplateController == null || SourceDataGrid == null) return;

                if (SearchTemplateController.SearchGroups.Count == 0)
                    SearchTemplateController.AddSearchGroup();

                var firstGroup = SearchTemplateController.SearchGroups[0];

                var searchType = SelectedSearchType;
                if (_temporarySearchTemplate != null)
                {
                    searchType = _temporarySearchTemplate.SearchType;
                    firstGroup.SearchTemplates.Remove(_temporarySearchTemplate);
                    _temporarySearchTemplate = null;
                }

                RemoveDefaultEmptyTemplates(firstGroup);

                var existingOfSameType = firstGroup.SearchTemplates
                    .Where(t => t.SearchType == searchType && t.HasCustomFilter)
                    .ToList();

                // NoInput types carry no value — passing the empty SearchText as SelectedValue
                // would still parse to "" and clutter the filter chip's display string.
                object selectedValue = ActiveSearchTypeRequiresInput ? GetEffectiveFilterValue() : null;
                var newTemplate = new SearchTemplate(SearchTemplateController.ColumnDataType)
                {
                    SearchType = searchType,
                    SelectedValue = selectedValue,
                    SearchTemplateController = SearchTemplateController,
                };
                SearchTemplateController.SubscribeToTemplateChanges(newTemplate);

                if (existingOfSameType.Any())
                    newTemplate.OperatorName = "Or";

                firstGroup.SearchTemplates.Add(newTemplate);
                SearchTemplateController.UpdateOperatorVisibility();
                SearchTemplateController.UpdateFilterExpression();
                SourceDataGrid.FilterItemsSource();
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddIncrementalFilter: {ex.Message}");
            }
        }

        private void UpdateSimpleFilter()
        {
            try
            {
                if (SearchTemplateController == null || SourceDataGrid == null) return;
                if (!HasAnyInputValue() || _temporarySearchTemplate == null) return;

                _temporarySearchTemplate.SelectedValue = GetEffectiveFilterValue();
                _temporarySearchTemplate.SearchType = SelectedSearchType;

                SearchTemplateController.UpdateFilterExpression();
                SourceDataGrid.FilterItemsSource();
                HasAdvancedFilter = SearchTemplateController.HasCustomExpression;
                SourceDataGrid.UpdateFilterPanel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateSimpleFilter: {ex.Message}");
            }
        }

        private void RemoveDefaultEmptyTemplates(SearchTemplateGroup group)
        {
            try
            {
                var toRemove = group.SearchTemplates
                    .Where(t => t != _temporarySearchTemplate && !t.HasCustomFilter)
                    .ToList();
                foreach (var t in toRemove)
                    group.SearchTemplates.Remove(t);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RemoveDefaultEmptyTemplates: {ex.Message}");
            }
        }

        private SearchType MapDefaultSearchTypeToSearchType(DefaultSearchType type) => type switch
        {
            DefaultSearchType.Contains => SearchType.Contains,
            DefaultSearchType.StartsWith => SearchType.StartsWith,
            DefaultSearchType.EndsWith => SearchType.EndsWith,
            DefaultSearchType.Equals => SearchType.Equals,
            _ => SearchType.Contains,
        };

        #endregion

        #region Popup

        /// <inheritdoc />
        public void ShowFilterEditor()
        {
            SourceDataGrid?.CommitEdit(DataGridEditingUnit.Cell, true);
            ShowFilterPopup();
        }

        private void ShowFilterPopup()
        {
            try
            {
                if (SourceDataGrid == null) return;
                if (SearchTemplateController == null)
                    InitializeSearchTemplateController();
                if (SearchTemplateController == null) return;

                if (_filterContent == null)
                {
                    _filterContent = new ColumnFilterEditor
                    {
                        SearchTemplateController = SearchTemplateController,
                        DataContext = this,
                    };
                    _filterContent.FiltersApplied += OnFiltersApplied;
                    _filterContent.FiltersCleared += OnFiltersCleared;
                }
                else if (_filterContent.SearchTemplateController != SearchTemplateController)
                {
                    _filterContent.SearchTemplateController = SearchTemplateController;
                }

                if (_filterPopup == null)
                {
                    _filterPopup = new Popup
                    {
                        Child = _filterContent,
                        PlacementTarget = this,
                        Placement = PlacementMode.Bottom,
                        AllowsTransparency = true,
                        PopupAnimation = PopupAnimation.Fade,
                        StaysOpen = false,
                        HorizontalOffset = _filterContent.HorizontalOffset,
                        VerticalOffset = _filterContent.VerticalOffset,
                    };
                    _filterPopup.KeyDown += OnPopupKeyDown;
                    _filterPopup.Closed += OnPopupClosed;
                }
                else
                {
                    _filterPopup.PlacementTarget = this;
                    _filterPopup.HorizontalOffset = _filterContent.HorizontalOffset;
                    _filterPopup.VerticalOffset = _filterContent.VerticalOffset;
                }

                _filterPopup.IsOpen = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ShowFilterPopup: {ex.Message}");
                if (_filterPopup != null) _filterPopup.IsOpen = false;
            }
        }

        private void OnPopupKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _filterPopup.IsOpen = false;
                e.Handled = true;
            }
        }

        private void OnPopupClosed(object sender, EventArgs e) { }

        private void OnFiltersApplied(object sender, EventArgs e)
        {
            UpdateHasActiveFilterState();
            SourceDataGrid?.UpdateFilterPanel();
        }

        private void OnFiltersCleared(object sender, EventArgs e)
        {
            UpdateHasActiveFilterState();
            SourceDataGrid?.UpdateFilterPanel();
        }

        #endregion
    }
}
