using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using WWControls.Core;

namespace WWControls.Wpf.Grids
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
                HandleEnterCommit(noInput: false);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Tab && !string.IsNullOrWhiteSpace(SearchText))
            {
                // Commit on Tab so leaving doesn't drop an in-flight filter. Not marked Handled
                // — the host's bubble OnKeyDown hands Tab to FilterRowNavigator after. Capture
                // the snapshot so a subsequent Enter on return treats this as a re-press.
                CommitSearchText();
                CaptureLastCommittedSnapshot();
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
        /// <see cref="BaseEditorSettings.CreateFilterEditor"/> — works across every editor shape.
        /// </summary>
        private void OnFilterEditorPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // ComboBox editors have dropdown-state-aware key semantics that don't fit the
            // text/typed-editor model below — route them to a dedicated handler.
            if (sender is ComboBox combo)
            {
                OnComboBoxFilterEditorPreviewKeyDown(combo, e);
                return;
            }

            if (e.Key == Key.Escape)
            {
                ClearSearchTextAndTemporaryFilter();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                bool noInput = !ActiveSearchTypeRequiresInput;
                if (!noInput && !HasAnyInputValue()) return;
                HandleEnterCommit(noInput);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Tab && (HasAnyInputValue() || !ActiveSearchTypeRequiresInput))
            {
                // Commit on Tab so leaving doesn't drop an in-flight filter. Not marked Handled
                // — the host's bubble OnKeyDown hands Tab to FilterRowNavigator after. Capture
                // the snapshot so a subsequent Enter on return treats this as a re-press.
                CommitSearchText();
                CaptureLastCommittedSnapshot();
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
        /// Dropdown-state-aware key handling for a ComboBox filter editor.
        /// <list type="bullet">
        ///   <item>Open — Space hides the popup; Tab closes it so the host's Tab navigation can
        ///   run; Enter / Escape / Up / Down fall through to native ComboBox handling (Enter
        ///   commits the highlighted item and closes, Escape closes + reverts, arrows move the
        ///   highlight). Leaving Enter unhandled is what actually commits the selection — closing
        ///   the popup programmatically would discard the highlight.</item>
        ///   <item>Closed — Enter / Space open the popup; Left / Right step to the adjacent filter
        ///   cell instead of cycling the selection; Down hands off to the first data row; Escape
        ///   clears the filter.</item>
        /// </list>
        /// </summary>
        private void OnComboBoxFilterEditorPreviewKeyDown(ComboBox combo, KeyEventArgs e)
        {
            if (combo.IsDropDownOpen)
            {
                if (e.Key == Key.Space)
                {
                    combo.IsDropDownOpen = false;
                    e.Handled = true;
                }
                else if (e.Key == Key.Tab)
                {
                    combo.IsDropDownOpen = false;
                    combo.Focus();
                    // Not Handled — the host's bubble Tab handler still navigates.
                }
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                case Key.Space:
                    combo.IsDropDownOpen = true;
                    e.Handled = true;
                    break;
                case Key.Left:
                case Key.Right:
                    // Consume even at the row edge so the arrow never falls through to the
                    // ComboBox's default selection cycling.
                    FilterRowNavigator.TryNavigate(this, e);
                    e.Handled = true;
                    break;
                case Key.Down:
                    FilterRowNavigator.TryNavigate(this, e);
                    break;
                case Key.Escape:
                    ClearSearchTextAndTemporaryFilter();
                    e.Handled = true;
                    break;
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
                    SourceDataGrid?.UpdateFilterSummaryPanel();
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
                ClearLastCommittedSnapshot();

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
                SourceDataGrid?.UpdateFilterSummaryPanel();

                if (wasNoInput && !IsCheckboxColumn)
                    ResetSelectedSearchTypeToDefault();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ClearSearchTextAndTemporaryFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Enter handler shared by the legacy <c>PART_SearchTextBox</c> and the materialized
        /// filter editor. When live filtering is on, Enter promotes the live preview to a
        /// permanent rule and refocuses for chaining. When live filtering is off, Enter has
        /// two stages: the first applies the typed input as a temp-template preview (the
        /// "live" filter the user opted out of); a re-press on unchanged input promotes that
        /// preview to a permanent rule and refocuses. NoInput operators always commit
        /// permanent — there's no input to compare.
        /// </summary>
        private void HandleEnterCommit(bool noInput)
        {
            bool commitPermanent = noInput
                || EffectiveIsLiveFilteringEnabled
                || IsEnterRePressWithUnchangedInput();

            if (commitPermanent)
            {
                CreatePermanentFilterAndRefocus();
                ClearLastCommittedSnapshot();
                if (noInput)
                    ResetSelectedSearchTypeToDefault();
            }
            else
            {
                CommitSearchText();
                CaptureLastCommittedSnapshot();
            }
        }

        private void CaptureLastCommittedSnapshot()
        {
            _lastCommittedSearchText = SearchText;
            _lastCommittedSearchValue = SearchValue;
        }

        private void ClearLastCommittedSnapshot()
        {
            _lastCommittedSearchText = null;
            _lastCommittedSearchValue = null;
        }

        private bool IsEnterRePressWithUnchangedInput()
        {
            if (!HasTemporaryTemplate) return false;
            if (!string.Equals(_lastCommittedSearchText ?? string.Empty,
                               SearchText ?? string.Empty,
                               StringComparison.Ordinal))
                return false;
            return Equals(_lastCommittedSearchValue, SearchValue);
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
                SourceDataGrid.UpdateFilterSummaryPanel();
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
                SourceDataGrid.UpdateFilterSummaryPanel();
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
                    _filterContent = new ColumnFilterPopup
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
            SourceDataGrid?.UpdateFilterSummaryPanel();
        }

        private void OnFiltersCleared(object sender, EventArgs e)
        {
            UpdateHasActiveFilterState();
            SourceDataGrid?.UpdateFilterSummaryPanel();
        }

        #endregion
    }
}
