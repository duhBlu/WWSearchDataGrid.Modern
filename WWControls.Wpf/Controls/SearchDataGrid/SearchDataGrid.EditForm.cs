using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace WWControls.Wpf
{
    public partial class SearchDataGrid
    {
        #region Edit Form (Inline / InlineHideRow)

        private object _editFormItem;
        private DataGridColumn _editFormFocusColumn;
        private bool _isEditFormOpen;
        private bool _editFormHostingApplied;
        private EditFormPresenter _editFormPresenter; // the open form, for focus / dirty / confirmation

        #region Dependency Properties

        /// <summary>
        /// Selects how full-row editing is presented — see <see cref="Wpf.EditFormShowMode"/>.
        /// <see cref="Wpf.EditFormShowMode.None"/> (the default) keeps the column-aligned
        /// "edit entire row" strip; the other modes show the edit <em>form</em> for the row instead.
        /// When this is not <c>None</c>, the row promotes into the form at the moment
        /// <see cref="RowEditTrigger"/> fires (or on <see cref="ShowEditForm(object)"/>). Switching
        /// back to <c>None</c> while a form is open cancels that edit.
        /// </summary>
        public static readonly DependencyProperty EditFormShowModeProperty =
            DependencyProperty.Register(nameof(EditFormShowMode), typeof(EditFormShowMode), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(EditFormShowMode.None, OnEditFormShowModeChanged));

        /// <summary>CLR accessor for <see cref="EditFormShowModeProperty"/>.</summary>
        public EditFormShowMode EditFormShowMode
        {
            get => (EditFormShowMode)GetValue(EditFormShowModeProperty);
            set => SetValue(EditFormShowModeProperty, value);
        }

        private static void OnEditFormShowModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid) return;
            if ((EditFormShowMode)e.NewValue == EditFormShowMode.None)
            {
                if (grid._isEditFormOpen)
                    grid.CancelEditForm();
            }
            else
            {
                grid.EnsureEditFormHosting();
            }
        }

        /// <summary>
        /// Optional developer-supplied form layout used by all modes. Its <c>DataContext</c> is the
        /// editing row item; place <see cref="EditFormEditor"/> elements (by <c>FieldName</c>) to drop
        /// in each column's editor. When unset, the form auto-generates a caption/editor layout from
        /// the grid's edit-form-visible columns.
        /// </summary>
        public static readonly DependencyProperty EditFormTemplateProperty =
            DependencyProperty.Register(nameof(EditFormTemplate), typeof(DataTemplate), typeof(SearchDataGrid),
                new PropertyMetadata(null));

        /// <summary>CLR accessor for <see cref="EditFormTemplateProperty"/>.</summary>
        public DataTemplate EditFormTemplate
        {
            get => (DataTemplate)GetValue(EditFormTemplateProperty);
            set => SetValue(EditFormTemplateProperty, value);
        }

        /// <summary>
        /// Whether moving focus out of a form holding unsaved changes prompts before abandoning the
        /// edit — see <see cref="Wpf.EditFormPostConfirmationMode"/>. Default
        /// <see cref="Wpf.EditFormPostConfirmationMode.None"/> (no prompt).
        /// </summary>
        public static readonly DependencyProperty EditFormPostConfirmationModeProperty =
            DependencyProperty.Register(nameof(EditFormPostConfirmationMode), typeof(EditFormPostConfirmationMode), typeof(SearchDataGrid),
                new PropertyMetadata(EditFormPostConfirmationMode.None));

        /// <summary>CLR accessor for <see cref="EditFormPostConfirmationModeProperty"/>.</summary>
        public EditFormPostConfirmationMode EditFormPostConfirmationMode
        {
            get => (EditFormPostConfirmationMode)GetValue(EditFormPostConfirmationModeProperty);
            set => SetValue(EditFormPostConfirmationModeProperty, value);
        }

        /// <summary>
        /// A static caption shown in the form's header bar. For a per-row caption bound to the
        /// edited item, set <see cref="EditFormCaptionBinding"/> instead.
        /// </summary>
        public static readonly DependencyProperty EditFormCaptionProperty =
            DependencyProperty.Register(nameof(EditFormCaption), typeof(object), typeof(SearchDataGrid),
                new PropertyMetadata(null));

        /// <summary>CLR accessor for <see cref="EditFormCaptionProperty"/>.</summary>
        public object EditFormCaption
        {
            get => GetValue(EditFormCaptionProperty);
            set => SetValue(EditFormCaptionProperty, value);
        }

        /// <summary>
        /// A binding evaluated against the edited row item to produce the form's header caption
        /// (e.g. <c>new Binding("Name")</c>). When set, it is applied to the open form's caption,
        /// overriding the static <see cref="EditFormCaption"/>. This is a plain CLR property, NOT a
        /// dependency property — a <see cref="BindingBase"/>-typed DP would establish a binding
        /// expression instead of capturing the literal binding (the same reason
        /// <see cref="ColumnDataBase.Binding"/> is a CLR property).
        /// </summary>
        public BindingBase EditFormCaptionBinding { get; set; }

        #endregion

        /// <summary>True while a row is open in the edit form.</summary>
        public bool IsEditFormOpen => _isEditFormOpen;

        /// <summary>The row item currently open in the edit form, or <c>null</c>.</summary>
        public object EditFormItem => _editFormItem;

        #region Begin / Commit / Cancel

        /// <summary>
        /// Opens <paramref name="item"/> in the edit form programmatically (works even when
        /// <see cref="RowEditTrigger"/> is <see cref="RowEditTrigger.Never"/>). No-op when
        /// <see cref="EditFormShowMode"/> is <see cref="Wpf.EditFormShowMode.None"/>.
        /// </summary>
        public void ShowEditForm(object item) => BeginEditForm(item, null);

        /// <summary>
        /// Opens <paramref name="item"/> in the edit form and hands focus to the editor for
        /// <paramref name="focusColumn"/> (the cell the user was on), falling back to the first
        /// editor. Routed here from <see cref="BeginRowEdit(object, DataGridColumn)"/> when
        /// <see cref="EditFormShowMode"/> is not <c>None</c>.
        /// </summary>
        internal void BeginEditForm(object item, DataGridColumn focusColumn)
        {
            if (EditFormShowMode == EditFormShowMode.None || _isEditFormOpen)
                return;
            if (!IsRowEditable(item))
                return;

            EnsureEditFormHosting();

            // Land any in-flight cell edit into the item, keeping the grid's open row transaction
            // (IEditableObject.BeginEdit was raised when the cell first entered edit).
            try { CommitEdit(DataGridEditingUnit.Cell, true); }
            catch (Exception ex) { Debug.WriteLine($"[EditForm] cell pre-commit failed: {ex.Message}"); }

            _editFormItem = item;
            _editFormFocusColumn = focusColumn;
            _isEditFormOpen = true;

            SetRowContainerEditing(item, true);

            // Realize the row (it is usually in view — the user clicked it) and open its details.
            ScrollIntoView(item);
            ApplyEditFormRowState(item, true);

            // After layout settles, the details presenter has realized the form — hand off focus,
            // reset the dirty flag for the new session, apply the caption binding, and wire the
            // focus-leave confirmation.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_isEditFormOpen) return;
                ApplyEditFormRowState(item, true); // re-assert after layout in case details just realized
                var presenter = FindEditFormPresenter(item);
                if (presenter != null)
                {
                    presenter.ResetDirty();
                    ApplyEditFormCaptionBinding(presenter);
                    HookEditFormFocusLeave(presenter);
                    presenter.FocusEditorForColumn(focusColumn);
                }
            }), DispatcherPriority.Loaded);

            RowEditStarted?.Invoke(this, new RowEditEventArgs(item, false));
        }

        /// <summary>
        /// Commits the open edit form as a unit — pushes the focused editor's value, then ends the
        /// grid's row transaction (<see cref="System.ComponentModel.IEditableObject.EndEdit"/>). If
        /// the commit is blocked (e.g. the validation gate), the form stays open.
        /// </summary>
        public void CommitEditForm()
        {
            if (!_isEditFormOpen)
                return;

            var item = _editFormItem;

            // The focused editor's TwoWay/LostFocus binding may not have pushed yet — force it.
            if (Keyboard.FocusedElement is FrameworkElement focused)
                ForceBindingUpdate(focused);

            bool committed;
            try
            {
                committed = CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditForm] CommitEdit(Row) threw: {ex.Message}");
                committed = false;
            }

            if (!committed)
                return; // Keep the form open so the user can fix whatever blocked the commit.

            EndEditForm();
            RowEditEnded?.Invoke(this, new RowEditEventArgs(item, true));
        }

        /// <summary>
        /// Cancels the open edit form, reverting every field as a unit via the grid's row
        /// transaction (<see cref="System.ComponentModel.IEditableObject.CancelEdit"/>).
        /// </summary>
        public void CancelEditForm()
        {
            if (!_isEditFormOpen)
                return;

            var item = _editFormItem;
            try
            {
                CancelEdit(DataGridEditingUnit.Row);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditForm] CancelEdit(Row) threw: {ex.Message}");
            }

            EndEditForm();
            RowEditEnded?.Invoke(this, new RowEditEventArgs(item, false));
        }

        /// <summary>Tears down the form and clears edit state. Shared by commit and cancel.</summary>
        private void EndEditForm()
        {
            var item = _editFormItem;

            UnhookEditFormFocusLeave();
            ApplyEditFormRowState(item, false);
            SetRowContainerEditing(item, false);

            _isEditFormOpen = false;
            _editFormItem = null;
            _editFormFocusColumn = null;

            // Return focus to the grid so keyboard navigation resumes on the row that was edited.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (item != null && ItemContainerGenerator.ContainerFromItem(item) is DataGridRow row)
                    row.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                else
                    Focus();
            }), DispatcherPriority.Input);
        }

        #endregion

        #region Hosting (RowDetails)

        /// <summary>
        /// Lazily wires the row-details host that carries the form: assigns the keyed
        /// <see cref="DataGrid.RowDetailsTemplate"/> and pins
        /// <see cref="DataGrid.RowDetailsVisibilityMode"/> to <c>Collapsed</c> so no row shows
        /// details until <see cref="ApplyEditFormRowState"/> flips the editing row on. Idempotent.
        /// </summary>
        private void EnsureEditFormHosting()
        {
            // Resolve the host template lazily, retrying on each call: the first call can run at
            // XAML-parse time before the theme resource is reachable, and we must not latch a null.
            if (RowDetailsTemplate == null)
                RowDetailsTemplate = TryFindResource(ThemeKeys.GridSearchDataGridEditFormRowDetailsTemplate) as DataTemplate;

            // No row shows details until ApplyEditFormRowState flips the editing row on.
            RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;

            // Mark the host wired so the container overrides re-apply open state during virtualization.
            _editFormHostingApplied = true;
        }

        /// <summary>
        /// Opens or closes the form for <paramref name="item"/>'s container: shows/hides its
        /// row-details area and, in <see cref="Wpf.EditFormShowMode.InlineHideRow"/>, hides the
        /// row's own cells while the form is open. No-op when the container isn't realized — the
        /// state is re-applied in <c>PrepareContainerForItemOverride</c> when it scrolls into view.
        /// </summary>
        private void ApplyEditFormRowState(object item, bool open)
        {
            if (item == null)
                return;
            if (ItemContainerGenerator.ContainerFromItem(item) is not SearchDataGridRow row)
                return;

            ApplyEditFormRowState(row, open);
        }

        /// <summary>Container-level form-state apply, shared with the virtualization re-apply path.</summary>
        internal void ApplyEditFormRowState(SearchDataGridRow row, bool open)
        {
            if (row == null)
                return;

            row.DetailsVisibility = open ? Visibility.Visible : Visibility.Collapsed;
            row.SetCellsHidden(open && EditFormShowMode == EditFormShowMode.InlineHideRow);
        }

        /// <summary>
        /// True when <paramref name="item"/> is the row currently open in the edit form. Used by the
        /// container overrides to re-apply the open state when a recycled row scrolls back in.
        /// </summary>
        internal bool IsEditFormItem(object item) => _isEditFormOpen && Equals(item, _editFormItem);

        private EditFormPresenter FindEditFormPresenter(object item)
        {
            if (item == null)
                return null;
            if (ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow row)
                return null;
            return VisualTreeHelperMethods.FindVisualDescendant<EditFormPresenter>(row);
        }

        private void ApplyEditFormCaptionBinding(EditFormPresenter presenter)
        {
            if (presenter == null || EditFormCaptionBinding == null)
                return;
            // Resolves against the presenter's inherited DataContext (the editing row item).
            BindingOperations.SetBinding(presenter, EditFormPresenter.CaptionProperty, EditFormCaptionBinding);
        }

        #endregion

        #region Focus-leave confirmation

        /// <summary>
        /// Wires the focus-leave confirmation on the open form. Replaces any prior hook so only the
        /// live presenter is observed (one form is open at a time).
        /// </summary>
        private void HookEditFormFocusLeave(EditFormPresenter presenter)
        {
            UnhookEditFormFocusLeave();
            _editFormPresenter = presenter;
            if (presenter != null)
                presenter.PreviewLostKeyboardFocus += OnEditFormPreviewLostKeyboardFocus;
        }

        private void UnhookEditFormFocusLeave()
        {
            if (_editFormPresenter != null)
                _editFormPresenter.PreviewLostKeyboardFocus -= OnEditFormPreviewLostKeyboardFocus;
            _editFormPresenter = null;
        }

        /// <summary>
        /// Traps focus leaving a dirty form when <see cref="EditFormPostConfirmationMode"/> requires
        /// a prompt: freezes the focus change (so nothing commits behind the dialog) and defers the
        /// modal off the focus-change callback.
        /// </summary>
        private void OnEditFormPreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!_isEditFormOpen || _editFormPresenter == null)
                return;
            if (EditFormPostConfirmationMode == EditFormPostConfirmationMode.None || !_editFormPresenter.IsDirty)
                return;
            // Focus moving within the form (e.g. between editors, to Update/Cancel) is fine.
            if (e.NewFocus is DependencyObject next && IsVisualDescendantOf(next, _editFormPresenter))
                return;

            e.Handled = true; // keep focus inside the form until the user answers
            Dispatcher.BeginInvoke(new Action(PromptEditFormConfirm), DispatcherPriority.Input);
        }

        private void PromptEditFormConfirm()
        {
            if (!_isEditFormOpen || _editFormPresenter == null)
                return;

            var buttons = EditFormPostConfirmationMode == EditFormPostConfirmationMode.YesNoCancel
                ? MessageBoxButton.YesNoCancel
                : MessageBoxButton.YesNo;

            var result = MessageBox.Show(
                "Do you want to cancel editing?",
                "Edit Form",
                buttons,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                CancelEditForm();
            }
            else
            {
                // No / Cancel — keep editing; return focus to the form.
                _editFormPresenter?.FocusEditorForColumn(_editFormFocusColumn);
            }
        }

        #endregion

        #endregion
    }
}
