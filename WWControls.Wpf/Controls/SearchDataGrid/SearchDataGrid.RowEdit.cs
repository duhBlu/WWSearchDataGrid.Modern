using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WWControls.Wpf.Grids
{
    public partial class SearchDataGrid
    {
        #region Full-Row ("Edit Entire Row") Edit Mode

        // Template parts hosting the dimming overlay and the bright, column-aligned editor strip.
        private FrameworkElement _rowEditOverlay;   // PART_RowEditOverlay — dims the whole grid
        private FrameworkElement _rowEditDim;        // PART_RowEditDim — swallows clicks outside the row
        private Canvas _rowEditHost;                 // PART_RowEditHost — positions the strip at the row
        private RowEditPresenter _rowEditPresenter;  // PART_RowEditPresenter — the editors + action bar

        private object _rowEditItem;
        private DataGridColumn _rowEditFocusColumn;
        private bool _isRowEditing;

        // One-shot detach for the OnCellValueChange dirty listener wired to the open cell editor.
        private Action _rowEditDirtyDetach;

        // Last geometry applied to the strip, so the per-layout-pass reposition only re-applies on a
        // real change (avoids layout thrash). NaN forces the first apply.
        private bool _rowEditLayoutHooked;
        private double _rowEditLeft = double.NaN;
        private double _rowEditTop = double.NaN;
        private double _rowEditWidth = double.NaN;
        private double _rowEditHeight = double.NaN;

        #region Dependency Property

        /// <summary>
        /// Gates when a row promotes into full-row ("edit entire row") edit mode — see
        /// <see cref="Wpf.RowEditTrigger"/>. Defaults to <see cref="Wpf.RowEditTrigger.Never"/>
        /// (the stock single-cell editing). Switching back to <c>Never</c> while a row is open
        /// cancels that edit.
        /// </summary>
        public static readonly DependencyProperty RowEditTriggerProperty =
            DependencyProperty.Register(nameof(RowEditTrigger), typeof(RowEditTrigger), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(RowEditTrigger.Never, OnRowEditTriggerChanged));

        /// <summary>CLR accessor for <see cref="RowEditTriggerProperty"/>.</summary>
        public RowEditTrigger RowEditTrigger
        {
            get => (RowEditTrigger)GetValue(RowEditTriggerProperty);
            set => SetValue(RowEditTriggerProperty, value);
        }

        private static void OnRowEditTriggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid && (RowEditTrigger)e.NewValue == RowEditTrigger.Never && grid._isRowEditing)
                grid.CancelRowEdit();
        }

        #endregion

        /// <summary>True while a row is open in full-row edit mode.</summary>
        public bool IsRowEditing => _isRowEditing;

        /// <summary>The row item currently open in full-row edit mode, or <c>null</c>.</summary>
        public object RowEditItem => _rowEditItem;

        /// <summary>Raised when a row enters full-row edit mode.</summary>
        public event EventHandler<RowEditEventArgs> RowEditStarted;

        /// <summary>
        /// Raised when a full-row edit ends — <see cref="RowEditEventArgs.Committed"/> is <c>true</c>
        /// for Update, <c>false</c> for Cancel.
        /// </summary>
        public event EventHandler<RowEditEventArgs> RowEditEnded;

        #region Template Wiring

        /// <summary>
        /// Captures the row-edit overlay template parts and wires the dim's input-trapping handlers.
        /// Called from <see cref="OnApplyTemplate"/>.
        /// </summary>
        private void InitializeRowEditParts()
        {
            if (_rowEditOverlay != null)
            {
                _rowEditOverlay.PreviewMouseDown -= OnRowEditOverlayPreviewMouseDown;
                _rowEditOverlay.PreviewMouseWheel -= OnRowEditOverlayPreviewMouseWheel;
            }

            _rowEditOverlay = GetTemplateChild("PART_RowEditOverlay") as FrameworkElement;
            _rowEditDim = GetTemplateChild("PART_RowEditDim") as FrameworkElement;
            _rowEditHost = GetTemplateChild("PART_RowEditHost") as Canvas;
            _rowEditPresenter = GetTemplateChild("PART_RowEditPresenter") as RowEditPresenter;

            if (_rowEditOverlay != null)
            {
                _rowEditOverlay.Visibility = Visibility.Collapsed;
                // Swallow clicks anywhere on the overlay EXCEPT the bright strip, so clicking the
                // grid neither moves the current cell nor exits edit mode — the row stays open until
                // Update / Cancel. The strip's editors and buttons are let through.
                _rowEditOverlay.PreviewMouseDown += OnRowEditOverlayPreviewMouseDown;
                // Block the wheel everywhere over the overlay so the grid can't scroll the edited
                // row out from under the strip while a row is open.
                _rowEditOverlay.PreviewMouseWheel += OnRowEditOverlayPreviewMouseWheel;
            }
        }

        private void OnRowEditOverlayPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_rowEditPresenter != null && e.OriginalSource is DependencyObject src
                && IsVisualDescendantOf(src, _rowEditPresenter))
                return; // Let the strip's own editors / buttons handle the click.
            e.Handled = true;
        }

        private void OnRowEditOverlayPreviewMouseWheel(object sender, MouseWheelEventArgs e) => e.Handled = true;

        private static bool IsVisualDescendantOf(DependencyObject node, DependencyObject ancestor)
        {
            while (node != null)
            {
                if (ReferenceEquals(node, ancestor)) return true;
                node = VisualTreeHelper.GetParent(node) ?? LogicalTreeHelper.GetParent(node);
            }
            return false;
        }

        #endregion

        #region Triggers

        /// <summary>
        /// Promotes a row into full-row edit mode at the moment a cell editor opens
        /// (<see cref="Wpf.RowEditTrigger.OnCellEditorOpen"/>) or arms a dirty listener that promotes
        /// on the first value change (<see cref="Wpf.RowEditTrigger.OnCellValueChange"/>).
        /// </summary>
        private void OnPreparingCellForEditForRowEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (RowEditTrigger == RowEditTrigger.Never || _isRowEditing)
                return;

            var item = e.Row?.Item;
            if (!IsRowEditable(item))
                return;

            var column = e.Column;

            if (RowEditTrigger == RowEditTrigger.OnCellEditorOpen)
            {
                // Defer so WPF finishes opening the cell before we commit it into the item and take
                // over with the strip.
                Dispatcher.BeginInvoke(new Action(() => BeginRowEdit(item, column)), DispatcherPriority.Background);
            }
            else // OnCellValueChange
            {
                // Attach the dirty listener AFTER the editor seeds its initial value — the binding
                // that sets the editor's starting Text / SelectedItem raises a change event during
                // setup, which would otherwise promote the row the instant the cell opened.
                var editingElement = e.EditingElement;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!_isRowEditing)
                        HookCellDirty(editingElement, item, column);
                }), DispatcherPriority.Background);
            }
        }

        /// <summary>Detaches any armed dirty listener once the cell edit ends without a promotion.</summary>
        private void OnCellEditEndingForRowEdit(object sender, DataGridCellEditEndingEventArgs e)
        {
            _rowEditDirtyDetach?.Invoke();
            _rowEditDirtyDetach = null;
        }

        /// <summary>
        /// Arms a one-shot "value changed" listener on the open cell editor. The first change promotes
        /// the whole row — so the user can tab through cells without summoning the overlay, but the
        /// instant they edit something the row opens with that change already part of its transaction.
        /// </summary>
        private void HookCellDirty(FrameworkElement editingElement, object item, DataGridColumn column)
        {
            _rowEditDirtyDetach?.Invoke();
            _rowEditDirtyDetach = null;

            var editor = UnwrapEditingElement(editingElement);
            if (editor == null)
                return;

            void Promote()
            {
                _rowEditDirtyDetach?.Invoke();
                _rowEditDirtyDetach = null;
                if (_isRowEditing)
                    return;
                // Defer off the editor's own change event — BeginRowEdit commits the cell, which is
                // unsafe to do re-entrantly from inside a TextChanged / SelectionChanged callback.
                Dispatcher.BeginInvoke(new Action(() => BeginRowEdit(item, column)), DispatcherPriority.Background);
            }

            switch (editor)
            {
                case TextBox tb:
                    TextChangedEventHandler h = (_, _) => Promote();
                    tb.TextChanged += h;
                    _rowEditDirtyDetach = () => tb.TextChanged -= h;
                    break;
                case CheckBox cb:
                    RoutedEventHandler ch = (_, _) => Promote();
                    cb.Checked += ch; cb.Unchecked += ch; cb.Indeterminate += ch;
                    _rowEditDirtyDetach = () =>
                    {
                        cb.Checked -= ch; cb.Unchecked -= ch; cb.Indeterminate -= ch;
                    };
                    break;
                case ComboBox combo:
                    SelectionChangedEventHandler sh = (_, _) => Promote();
                    combo.SelectionChanged += sh;
                    _rowEditDirtyDetach = () => combo.SelectionChanged -= sh;
                    break;
                case DatePicker dp:
                    EventHandler<SelectionChangedEventArgs> dh = (_, _) => Promote();
                    dp.SelectedDateChanged += dh;
                    _rowEditDirtyDetach = () => dp.SelectedDateChanged -= dh;
                    break;
            }
        }

        #endregion

        #region Begin / Commit / Cancel

        /// <summary>
        /// Opens <paramref name="item"/> in full-row edit mode, focusing the first editable cell.
        /// No-op when row editing is disabled (<see cref="Wpf.RowEditTrigger.Never"/>), already
        /// active, or the item isn't an editable data row.
        /// </summary>
        public void BeginRowEdit(object item) => BeginRowEdit(item, null);

        /// <summary>
        /// Opens <paramref name="item"/> in full-row edit mode and hands focus to the editor for
        /// <paramref name="focusColumn"/> (the cell the user was on), falling back to the first editor.
        /// </summary>
        internal void BeginRowEdit(object item, DataGridColumn focusColumn)
        {
            // When an edit-form presentation is selected, full-row editing shows the form, not the
            // column-aligned strip. The RowEditTrigger gate still decides WHEN this runs (the caller
            // OnPreparingCellForEditForRowEdit honors RowEditTrigger.Never); this just swaps WHAT opens.
            if (EditFormShowMode != EditFormShowMode.None)
            {
                BeginEditForm(item, focusColumn);
                return;
            }

            if (RowEditTrigger == RowEditTrigger.Never || _isRowEditing)
                return;
            if (!IsRowEditable(item))
                return;
            if (_rowEditPresenter == null || _rowEditOverlay == null || _rowEditHost == null)
            {
                Debug.WriteLine("[RowEdit] BeginRowEdit skipped — overlay template parts not applied.");
                return;
            }

            // Land any in-flight cell edit into the item, keeping the grid's open row transaction
            // (IEditableObject.BeginEdit was raised when the cell first entered edit). For
            // OnCellEditorOpen this is a no-op (nothing typed yet); for OnCellValueChange it folds
            // the triggering change into the transaction so Cancel reverts it too.
            try { CommitEdit(DataGridEditingUnit.Cell, true); }
            catch (Exception ex) { Debug.WriteLine($"[RowEdit] cell pre-commit failed: {ex.Message}"); }

            _rowEditItem = item;
            _rowEditFocusColumn = focusColumn;
            _isRowEditing = true;

            SetRowContainerEditing(item, true);

            _rowEditPresenter.EditingItem = item;
            _rowEditOverlay.Visibility = Visibility.Visible;
            // The overlay was collapsed, so the presenter hasn't realized its template / built its
            // editors yet — force a layout pass before positioning and focusing.
            _rowEditOverlay.UpdateLayout();
            // Track every layout pass so the strip follows window / splitter resizes and
            // star-column redistribution while the row is open.
            HookRowEditLayout(true);
            PositionRowEditOverlay();

            // Re-position and move focus after layout settles (column widths, strip height).
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_isRowEditing) return;
                PositionRowEditOverlay();
                _rowEditPresenter.FocusEditorForColumn(focusColumn);
            }), DispatcherPriority.Loaded);

            RowEditStarted?.Invoke(this, new RowEditEventArgs(item, false));
        }

        /// <summary>
        /// Commits the open full-row edit as a unit — pushes the focused editor's value, then ends
        /// the grid's row transaction (<see cref="System.ComponentModel.IEditableObject.EndEdit"/>).
        /// If the commit is blocked (e.g. the validation gate), the row stays open.
        /// </summary>
        public void CommitRowEdit()
        {
            if (!_isRowEditing)
                return;

            var item = _rowEditItem;

            // The focused strip editor's TwoWay/LostFocus binding may not have pushed yet (e.g. a
            // keyboard-driven commit) — force it before committing the row.
            if (Keyboard.FocusedElement is FrameworkElement focused)
                ForceBindingUpdate(focused);

            bool committed;
            try
            {
                committed = CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RowEdit] CommitEdit(Row) threw: {ex.Message}");
                committed = false;
            }

            if (!committed)
                return; // Keep the row open so the user can fix whatever blocked the commit.

            EndRowEdit();
            RowEditEnded?.Invoke(this, new RowEditEventArgs(item, true));
        }

        /// <summary>
        /// Cancels the open full-row edit, reverting every cell as a unit via the grid's row
        /// transaction (<see cref="System.ComponentModel.IEditableObject.CancelEdit"/>).
        /// </summary>
        public void CancelRowEdit()
        {
            if (!_isRowEditing)
                return;

            var item = _rowEditItem;
            try
            {
                CancelEdit(DataGridEditingUnit.Row);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RowEdit] CancelEdit(Row) threw: {ex.Message}");
            }

            EndRowEdit();
            RowEditEnded?.Invoke(this, new RowEditEventArgs(item, false));
        }

        /// <summary>Tears down the overlay and clears edit state. Shared by commit and cancel.</summary>
        private void EndRowEdit()
        {
            var item = _rowEditItem;

            HookRowEditLayout(false);
            SetRowContainerEditing(item, false);

            if (_rowEditPresenter != null)
                _rowEditPresenter.EditingItem = null;
            if (_rowEditOverlay != null)
                _rowEditOverlay.Visibility = Visibility.Collapsed;

            _isRowEditing = false;
            _rowEditItem = null;
            _rowEditFocusColumn = null;
            _rowEditLeft = _rowEditTop = _rowEditWidth = _rowEditHeight = double.NaN;

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

        #region Helpers

        /// <summary>
        /// True when <paramref name="item"/> is a real, editable data row — not a group sentinel, the
        /// new-item placeholder, or the row currently being added.
        /// </summary>
        private bool IsRowEditable(object item)
        {
            if (item == null) return false;
            if (item is GroupHeaderRow || item is GroupFooterRow) return false;
            if (IsPlaceholderItem(item)) return false;
            if (IsItemBeingAdded(item)) return false;
            return true;
        }

        private void SetRowContainerEditing(object item, bool editing)
        {
            if (item == null) return;
            if (ItemContainerGenerator.ContainerFromItem(item) is SearchDataGridRow row)
                row.SetRowEditing(editing);
        }

        /// <summary>
        /// Subscribes (or unsubscribes) the per-layout-pass reposition that keeps the editor strip
        /// aligned to its row as the grid resizes. Idempotent.
        /// </summary>
        private void HookRowEditLayout(bool hook)
        {
            if (hook == _rowEditLayoutHooked)
                return;
            _rowEditLayoutHooked = hook;
            if (hook)
                LayoutUpdated += OnRowEditLayoutUpdated;
            else
                LayoutUpdated -= OnRowEditLayoutUpdated;
        }

        private void OnRowEditLayoutUpdated(object sender, EventArgs e)
        {
            if (_isRowEditing)
                PositionRowEditOverlay();
            else
                HookRowEditLayout(false);
        }

        /// <summary>
        /// Positions the bright editor strip exactly over the row being edited and sizes it to the
        /// data area (excluding the row-header gutter, which the column-aligned panel doesn't cover).
        /// Runs on every layout pass while a row is open — so it re-applies on window / splitter
        /// resize and star-column redistribution — but only when the geometry actually changed, so
        /// it doesn't thrash layout (mirrors the resync in <see cref="ColumnAlignedRowPresenter"/>).
        /// </summary>
        private void PositionRowEditOverlay()
        {
            if (!_isRowEditing || _rowEditHost == null || _rowEditPresenter == null || _rowEditItem == null)
                return;

            if (ItemContainerGenerator.ContainerFromItem(_rowEditItem) is not DataGridRow container)
                return;

            Point topLeft;
            try
            {
                topLeft = container.TranslatePoint(new Point(0, 0), _rowEditHost);
            }
            catch (InvalidOperationException)
            {
                return; // Not in a common visual tree yet — a later layout pass retries.
            }

            double rowHeaderWidth = RowHeaderActualWidth;
            double dataWidth = container.ActualWidth - rowHeaderWidth;
            if (dataWidth <= 0)
                dataWidth = Math.Max(0, _rowEditHost.ActualWidth - rowHeaderWidth);

            double left = topLeft.X + rowHeaderWidth;
            double top = topLeft.Y;
            double height = container.ActualHeight;

            // Skip when nothing moved — every WPF layout pass raises LayoutUpdated, and re-applying
            // unchanged values would invalidate layout again in a loop.
            if (Close(left, _rowEditLeft) && Close(top, _rowEditTop)
                && Close(dataWidth, _rowEditWidth) && Close(height, _rowEditHeight))
                return;

            _rowEditLeft = left;
            _rowEditTop = top;
            _rowEditWidth = dataWidth;
            _rowEditHeight = height;

            Canvas.SetLeft(_rowEditPresenter, left);
            Canvas.SetTop(_rowEditPresenter, top);
            _rowEditPresenter.Width = dataWidth;
            _rowEditPresenter.MinHeight = height;
        }

        private static bool Close(double a, double b) => Math.Abs(a - b) < 0.5;

        #endregion

        #endregion
    }

    /// <summary>Event data for <see cref="SearchDataGrid.RowEditStarted"/> / <see cref="SearchDataGrid.RowEditEnded"/>.</summary>
    public class RowEditEventArgs : EventArgs
    {
        public RowEditEventArgs(object item, bool committed)
        {
            Item = item;
            Committed = committed;
        }

        /// <summary>The row item that was edited.</summary>
        public object Item { get; }

        /// <summary>True when the edit was committed (Update), false when cancelled.</summary>
        public bool Committed { get; }
    }
}
