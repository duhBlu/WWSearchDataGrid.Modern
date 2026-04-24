using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace WWSearchDataGrid.Modern.WPF
{
    public partial class SearchDataGrid
    {
        #region Empty-State Horizontal Scroll

        private object _emptyStatePlaceholder;
        private Predicate<object> _realFilter;

        /// <summary>
        /// When all items are filtered out, lets one existing item through the filter
        /// so the DataGrid's internal panel still has a child and can calculate horizontal
        /// scroll extent. The placeholder row is rendered at zero height — completely
        /// invisible and non-interactive.
        ///
        /// When <see cref="DataGrid.CanUserAddRows"/> is true the DataGrid already keeps
        /// a new-item row that anchors the scroll extent, so no placeholder is needed.
        /// </summary>
        private void InjectPlaceholderRowIfEmpty()
        {
            try
            {
                if (CanUserAddRows)
                {
                    ClearPlaceholderState();
                    return;
                }

                if (Items.Count == 0 && originalItemsSource != null)
                {
                    // Pick the first item from the original source
                    object placeholder = FirstItemOrNull(originalItemsSource);
                    if (placeholder == null)
                        return;

                    _emptyStatePlaceholder = placeholder;
                    _realFilter = Items.Filter;

                    // Capture locals for the closure so it doesn't hold 'this'
                    var realFilter = _realFilter;
                    var sentinel = _emptyStatePlaceholder;

                    Items.Filter = item =>
                    {
                        if (ReferenceEquals(item, sentinel)) return true;
                        return realFilter?.Invoke(item) ?? true;
                    };
                    SearchFilter = Items.Filter;
                }
                else
                {
                    ClearPlaceholderState();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InjectPlaceholderRowIfEmpty: {ex.Message}");
            }
        }

        private void ClearPlaceholderState()
        {
            _emptyStatePlaceholder = null;
            _realFilter = null;
        }

        /// <summary>
        /// Returns true when the given item is the invisible scroll-anchor placeholder.
        /// </summary>
        internal bool IsPlaceholderItem(object item)
        {
            return _emptyStatePlaceholder != null
                && ReferenceEquals(item, _emptyStatePlaceholder);
        }

        /// <summary>
        /// Makes a <see cref="DataGridRow"/> invisible when it hosts the placeholder item.
        /// Called from <see cref="OnLoadingRow"/>.
        /// </summary>
        private void ConfigurePlaceholderRow(DataGridRow row)
        {
            row.Height = 0;
            row.MinHeight = 0;
            row.IsHitTestVisible = false;
            row.Focusable = false;
            row.IsEnabled = false;
            row.Tag = "__sdg_placeholder";
        }

        /// <summary>
        /// Restores normal properties so the row container can be recycled for real data.
        /// Called from <see cref="OnUnloadingRow"/>.
        /// </summary>
        private void ResetPlaceholderRow(DataGridRow row)
        {
            row.ClearValue(HeightProperty);
            row.ClearValue(MinHeightProperty);
            row.IsHitTestVisible = true;
            row.Focusable = true;
            row.IsEnabled = true;
            row.Tag = null;
        }

        protected override void OnUnloadingRow(DataGridRowEventArgs e)
        {
            base.OnUnloadingRow(e);

            if (e.Row.Tag is string tag && tag == "__sdg_placeholder")
            {
                ResetPlaceholderRow(e.Row);
            }

            // Clear row animation for clean recycling
            HandleRowAnimationOnUnloadingRow(e.Row);
        }

        private static object FirstItemOrNull(IEnumerable source)
        {
            if (source == null) return null;
            var enumerator = source.GetEnumerator();
            try
            {
                return enumerator.MoveNext() ? enumerator.Current : null;
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }

        #endregion
    }
}
