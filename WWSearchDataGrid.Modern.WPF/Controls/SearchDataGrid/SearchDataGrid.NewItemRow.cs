using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace WWSearchDataGrid.Modern.WPF
{
    public partial class SearchDataGrid
    {
        #region New Item Row

        /// <summary>
        /// True once <see cref="NewRowPosition"/> has been explicitly set (the change callback fired).
        /// Gates the load/source re-assert so a consumer that never touches the property — and drives
        /// <see cref="DataGrid.CanUserAddRows"/> directly — is left completely alone.
        /// </summary>
        private bool _newRowPositionEngaged;

        /// <summary>
        /// Where the new-item row sits — <see cref="NewRowPosition.Top"/>,
        /// <see cref="NewRowPosition.Bottom"/>, or <see cref="NewRowPosition.None"/> (adding disabled).
        /// A high-level wrapper over the base <see cref="DataGrid.CanUserAddRows"/> and the editable
        /// view's <see cref="IEditableCollectionView.NewItemPlaceholderPosition"/>.
        /// <para>
        /// Defaults to <see cref="NewRowPosition.Bottom"/>, matching the stock DataGrid (placeholder at
        /// the end when adds are allowed). The change callback never fires for the default value, so a
        /// consumer that drives <see cref="DataGrid.CanUserAddRows"/> directly is left untouched —
        /// this property only takes over once it is explicitly set.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty NewRowPositionProperty =
            DependencyProperty.Register(nameof(NewRowPosition), typeof(NewRowPosition), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(NewRowPosition.Bottom, OnNewRowPositionChanged));

        /// <summary>CLR accessor for <see cref="NewRowPositionProperty"/>.</summary>
        public NewRowPosition NewRowPosition
        {
            get => (NewRowPosition)GetValue(NewRowPositionProperty);
            set => SetValue(NewRowPositionProperty, value);
        }

        private static void OnNewRowPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (SearchDataGrid)d;
            grid._newRowPositionEngaged = true;
            grid.ApplyNewRowPosition();
        }

        /// <summary>
        /// Translates <see cref="NewRowPosition"/> into the base grid's <see cref="DataGrid.CanUserAddRows"/>
        /// and the editable view's <see cref="IEditableCollectionView.NewItemPlaceholderPosition"/>.
        /// <para>
        /// The stock DataGrid only ever drives the placeholder to <see cref="NewItemPlaceholderPosition.AtEnd"/>
        /// (adds on) or <see cref="NewItemPlaceholderPosition.None"/> (adds off) off its CanUserAddRows
        /// changes — it has no notion of a top placeholder. So <see cref="NewRowPosition.Top"/> is
        /// applied by flipping CanUserAddRows on first (which the grid resolves to AtEnd) and then
        /// overriding the view to <see cref="NewItemPlaceholderPosition.AtBeginning"/>. That override has
        /// to be re-asserted whenever the grid rebuilds the view, which is why
        /// <see cref="ReassertNewRowPosition"/> runs from OnItemsSourceChanged and the Loaded handler.
        /// </para>
        /// </summary>
        private void ApplyNewRowPosition()
        {
            var position = NewRowPosition;
            bool canAdd = position != NewRowPosition.None;

            Debug.WriteLine($"[NewRowDbg] ApplyNewRowPosition pos={position} canAdd={canAdd} " +
                $"CanUserAddRows(before)={CanUserAddRows} IsReadOnly={IsReadOnly} IsLoaded={IsLoaded} ItemsCount={Items.Count}");

            // Flip CanUserAddRows first: the base grid responds by forcing the placeholder to
            // AtEnd / None, which the AtBeginning override below then corrects for the Top case.
            if (CanUserAddRows != canAdd)
                SetCurrentValue(CanUserAddRowsProperty, canAdd);

            Debug.WriteLine($"[NewRowDbg] ApplyNewRowPosition CanUserAddRows(after)={CanUserAddRows}");

            if (canAdd)
                ApplyNewItemPlaceholderPosition(position);
        }

        /// <summary>
        /// Pushes the requested placeholder position onto the editable view. Guards the WPF rule that
        /// the position can't change during an add/edit transaction (it throws) and the case where the
        /// view doesn't support adds at all.
        /// <para>
        /// The Top case has to be forced: the base grid forces the placeholder to AtEnd on its own
        /// refreshes and only re-reads the view's position on a real change. If the view already
        /// reports AtBeginning — e.g. a set that landed before columns were generated at load — a
        /// plain re-set is a no-op and the row never renders, so cycle through None to raise a fresh
        /// collection reset that regenerates the placeholder row at the top.
        /// </para>
        /// </summary>
        private void ApplyNewItemPlaceholderPosition(NewRowPosition position)
        {
            if (Items is not IEditableCollectionView editableView)
            {
                Debug.WriteLine("[NewRowDbg] ApplyPlaceholder: Items is not IEditableCollectionView");
                return;
            }

            // WPF forbids changing the placeholder position mid-transaction.
            if (editableView.IsAddingNew || editableView.IsEditingItem)
            {
                Debug.WriteLine($"[NewRowDbg] ApplyPlaceholder: skipped mid-transaction " +
                    $"(IsAddingNew={editableView.IsAddingNew}, IsEditingItem={editableView.IsEditingItem})");
                return;
            }

            var target = position == NewRowPosition.Top
                ? NewItemPlaceholderPosition.AtBeginning
                : NewItemPlaceholderPosition.AtEnd;

            Debug.WriteLine($"[NewRowDbg] ApplyPlaceholder: pos={position} target={target} " +
                $"current={editableView.NewItemPlaceholderPosition} CanAddNew={editableView.CanAddNew} " +
                $"CanUserAddRows={CanUserAddRows}");

            try
            {
                if (target == NewItemPlaceholderPosition.AtBeginning
                    && editableView.NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning)
                {
                    // Already AtBeginning but possibly not rendered — force a real change so the
                    // view raises a reset and the grid regenerates the placeholder row at the top.
                    editableView.NewItemPlaceholderPosition = NewItemPlaceholderPosition.None;
                }

                if (editableView.NewItemPlaceholderPosition != target)
                    editableView.NewItemPlaceholderPosition = target;

                Debug.WriteLine($"[NewRowDbg] ApplyPlaceholder: after set current={editableView.NewItemPlaceholderPosition}");
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"[NewRowDbg] ApplyPlaceholder: threw {ex.Message}");
            }
        }

        /// <summary>
        /// Re-asserts the new-row position after the grid rebuilds its view or rows — a source swap,
        /// or the initial load (columns aren't generated until <c>Loaded</c>, so the position applied
        /// during binding is lost when the grid first builds its rows, and CanUserAddRows may not have
        /// coerced to its final value yet). Re-runs the full <see cref="ApplyNewRowPosition"/> so it
        /// behaves exactly like a runtime change — which is known to work. Gated on
        /// <see cref="_newRowPositionEngaged"/> so untouched grids are left alone, and skipped while
        /// grouping is active (the grid then shows a flat projection, not the editable collection).
        /// </summary>
        private void ReassertNewRowPosition()
        {
            if (_groupingActive || !_newRowPositionEngaged)
                return;

            ApplyNewRowPosition();
        }

        #endregion
    }
}
