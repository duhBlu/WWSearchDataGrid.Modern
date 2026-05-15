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
    /// Text + typed-value filtering for <see cref="ColumnFilterControl"/>. The flow mirrors
    /// <see cref="ColumnSearchBox"/> but drops the prefix-shortcut path entirely — the user
    /// picks the active <see cref="SearchType"/> via the visible
    /// <see cref="SearchTypeSelector"/>, and no prefix parsing runs.
    /// </summary>
    public partial class ColumnFilterControl
    {
        #region DP change

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnFilterControl ctl) return;
            if (ctl._filterPopup?.IsOpen == true) return;
            // Programmatic resets (NoInput transition, clear paths) manage the temp template
            // themselves — don't auto-clear it from this side-channel notification.
            if (ctl._suppressSearchTextSync) return;

            if (string.IsNullOrWhiteSpace((string)e.NewValue))
            {
                ctl.ClearTemporaryTemplate();
                return;
            }

            if (!ctl.EffectiveIsLiveFilteringEnabled)
                return;

            // Materialize the temporary template up front so the SelectedValue tracks the
            // typed text immediately — the actual filter rebuild is what gets debounced.
            // This keeps the filter-state flags (HasActiveFilter, etc.) coherent without
            // waiting for the timer to fire.
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
            // Programmatic resets (NoInput transition, clear paths) manage the temp template
            // themselves — don't auto-clear it from this side-channel notification.
            if (ctl._suppressSearchTextSync) return;

            // Typed editors (DatePicker / ComboBox / NumericUpDown) write through this DP.
            // Treat a null / empty value as "clear", anything else as "apply via the active
            // SearchType" — same shape as SearchText, just object-typed.
            if (e.NewValue == null
                || (e.NewValue is string s && string.IsNullOrWhiteSpace(s)))
            {
                ctl.ClearTemporaryTemplate();
                return;
            }

            ctl.CreateTemporaryTemplateImmediate();

            // Typed editors don't debounce — DatePicker commits on calendar click, ComboBox
            // on selection — so apply immediately regardless of EffectiveIsLiveFilteringEnabled.
            // Deferred mode is meaningful only for keystroke-by-keystroke text, which is
            // covered by OnSearchTextChanged above.
            ctl.UpdateSimpleFilter();
        }

        #endregion

        #region TextBox event handlers

        private void OnSearchTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            // The TextBox's Text binding to SearchText already propagates the value;
            // OnSearchTextChanged runs the debounce / filter creation. This hook only exists
            // to absorb the case where the popup is open (filter editor) — block our own
            // typing path from re-entering the controller while the popup is editing it.
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
                // Commit on Tab so leaving the editor doesn't drop an in-flight filter.
                // Don't mark Handled — the host's OnKeyDown (bubble) hands Tab to
                // FilterRowNavigator after this commit runs.
                CommitSearchText();
                return;
            }

            // Left / Right at caret boundary → step to adjacent filter cell.
            // Down → hand off to the first data row in the same column.
            // Up is intentionally not forwarded; nothing sits above the filter row.
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
        /// Generic Enter/Escape handler for the dynamic editor inside <c>PART_EditorHost</c>.
        /// The legacy <see cref="OnSearchTextBoxPreviewKeyDown"/> path only fires when the
        /// template exposes a <c>PART_SearchTextBox</c>, which the AutoFilterRow template no
        /// longer does — the editor (TextBox / DatePicker / ComboBox / NumericUpDown) is
        /// produced by <see cref="BaseEditSettings.CreateFilterEditor"/> at runtime. This
        /// handler is attached to whatever element <c>RefreshEditor</c> hosts so the
        /// commit-and-refocus gesture works for every editor shape.
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
                // NoInput types (IsNull, Today, AboveAverage, …) commit on Enter even without
                // a typed value — the auto-applied temp template is the user's intent. After
                // committing, the cell returns to its default search type so the next entry
                // starts on the column's normal operator (Contains / StartsWith / Equals).
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
                // Commit on Tab so leaving the editor doesn't drop an in-flight filter — for
                // NoInput types the temp template carries the filter intent even without
                // typed input. Don't mark Handled — the host's OnKeyDown (bubble) hands Tab
                // to FilterRowNavigator after this commit runs.
                CommitSearchText();
                return;
            }

            // Left / Right at caret boundary → step to adjacent filter cell. Non-TextBox
            // editors (DatePicker, ComboBox, NumericUpDown) keep their own arrow handling —
            // IsCaretAtTextBoundary returns false for non-TextBox senders. Down → hand off
            // to the first data row in the same column (single-line filter editors have no
            // line-down concept). Up is not forwarded; nothing sits above the filter row.
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
        /// Returns <c>true</c> when <paramref name="sender"/> is a <see cref="TextBox"/>
        /// with the caret at the boundary indicated by <paramref name="key"/> (start for
        /// <see cref="Key.Left"/>, end for <see cref="Key.Right"/>) and no active
        /// selection. Mirrors the boundary contract used by data-cell arrow exit
        /// (<c>BaseEditSettings.ExitCellViaArrow</c>) — selection-active and mid-text
        /// caret states let the editor consume the arrow for caret movement; only edge
        /// hits propagate as a cell-step request.
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
        /// Returns the value to use as the temporary template's <c>SelectedValue</c>. Strips
        /// mask literals when the column is configured with a display mask; otherwise just
        /// returns <see cref="SearchText"/> verbatim. Prefix parsing (legacy) is not invoked
        /// here — the active <see cref="SearchType"/> is read directly from
        /// <see cref="SelectedSearchType"/>.
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

        /// <summary>
        /// Returns the effective filter value — prefers <see cref="SearchValue"/> for typed
        /// editors, falls back to <see cref="SearchText"/> for the text-editor case.
        /// </summary>
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

                // Capture whether the cell was on a NoInput type before we touch anything:
                // resetting SelectedSearchType at the end of this method (only for NoInput)
                // gives the user a fresh-default cell after clearing IsNull / Today / etc.
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
                // NoInput types commit on the active SearchType alone — the temp template
                // already carries the filter intent. Don't gate them on HasAnyInputValue.
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
                    // Prefer the dynamic editor (current AutoFilterRow template) and fall
                    // back to the legacy PART_SearchTextBox slot — the latter is null in
                    // the current template but kept for any host that still exposes it.
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
                        IsLiveApplyEnabled = EffectiveIsLiveFilteringEnabled,
                    };
                    _filterContent.FiltersApplied += OnFiltersApplied;
                    _filterContent.FiltersCleared += OnFiltersCleared;
                }
                else if (_filterContent.SearchTemplateController != SearchTemplateController)
                {
                    _filterContent.SearchTemplateController = SearchTemplateController;
                    _filterContent.IsLiveApplyEnabled = EffectiveIsLiveFilteringEnabled;
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
