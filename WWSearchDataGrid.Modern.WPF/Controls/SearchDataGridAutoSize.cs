using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WWSearchDataGrid.Modern.WPF
{
    public partial class SearchDataGrid
    {
        #region Auto-Size Columns Support

        private static void OnAutoSizeColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                bool autoSize = (bool)e.NewValue;
                if (autoSize)
                    grid.EnableAutoSizeColumns();
                else
                    grid.DisableAutoSizeColumns();
            }
        }


        /// <summary>
        /// Enables automatic column sizing
        /// </summary>
        private void EnableAutoSizeColumns()
        {
            try
            {
                // Store original column widths
                _originalColumnWidths.Clear();
                foreach (var column in Columns)
                {
                    _originalColumnWidths[column] = column.Width;
                }

                // Apply auto-sizing to all columns
                ApplyAutoSizeToAllColumns();

                // Find and attach to the ScrollViewer for scroll event handling
                if (_scrollViewer == null)
                {
                    _scrollViewer = VisualTreeHelperMethods.FindVisualChild<ScrollViewer>(this);
                    if (_scrollViewer != null)
                    {
                        _scrollViewer.ScrollChanged += OnScrollChanged;
                    }
                }

                // Also listen to layout updates to catch new rows being displayed
                this.LayoutUpdated += OnLayoutUpdatedForAutoSize;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enabling auto-size columns: {ex.Message}");
            }
        }

        /// <summary>
        /// Disables automatic column sizing and restores original widths
        /// </summary>
        private void DisableAutoSizeColumns()
        {
            try
            {
                // Restore original column widths
                foreach (var kvp in _originalColumnWidths)
                {
                    if (Columns.Contains(kvp.Key))
                    {
                        kvp.Key.Width = kvp.Value;
                    }
                }
                _originalColumnWidths.Clear();

                // Detach event handlers
                if (_scrollViewer != null)
                {
                    _scrollViewer.ScrollChanged -= OnScrollChanged;
                }
                this.LayoutUpdated -= OnLayoutUpdatedForAutoSize;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disabling auto-size columns: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the Column Chooser window
        /// </summary>
        private void ShowColumnChooser()
        {
            try
            {
                // Don't show if the feature is disabled
                if (!IsColumnChooserEnabled)
                {
                    IsColumnChooserVisible = false;
                    return;
                }

                // Create the ColumnChooser instance if it doesn't exist
                if (_columnChooser == null)
                {
                    _columnChooser = new ColumnChooser
                    {
                        SourceDataGrid = this,
                        IsConfinedToGrid = IsColumnChooserConfinedToGrid
                    };

                    // When the column chooser window closes, update the property
                    _columnChooser.Unloaded += (s, e) =>
                    {
                        IsColumnChooserVisible = false;
                    };
                }

                // Show the non-modal window
                _columnChooser.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing column chooser: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides the Column Chooser window
        /// </summary>
        private void HideColumnChooser()
        {
            try
            {
                _columnChooser?.Close();
                _columnChooser = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error hiding column chooser: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies auto-sizing to all columns
        /// </summary>
        private void ApplyAutoSizeToAllColumns()
        {
            try
            {
                foreach (var column in Columns)
                {
                    column.Width = DataGridLength.Auto;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying auto-size to all columns: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles scroll events to update column sizing during scrolling
        /// </summary>
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                // Only re-measure if we've scrolled vertically (new rows visible)
                if (e.VerticalChange != 0 && AutoSizeColumns)
                {
                    // Use a small delay to avoid excessive updates during fast scrolling
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (AutoSizeColumns)
                        {
                            UpdateColumnSizes();
                        }
                    }), DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnScrollChanged for auto-size: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles layout updates to ensure columns stay auto-sized
        /// </summary>
        private void OnLayoutUpdatedForAutoSize(object sender, EventArgs e)
        {
            try
            {
                // This will be called frequently, so we need to be careful about performance
                // Only update if AutoSizeColumns is still enabled
                if (AutoSizeColumns)
                {
                    // Check if any column has lost its Auto width (could happen due to user interaction)
                    bool needsUpdate = false;
                    foreach (var column in Columns)
                    {
                        if (!column.Width.IsAuto)
                        {
                            needsUpdate = true;
                            break;
                        }
                    }

                    if (needsUpdate)
                    {
                        ApplyAutoSizeToAllColumns();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnLayoutUpdatedForAutoSize: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates column sizes to fit current content
        /// </summary>
        private void UpdateColumnSizes()
        {
            try
            {
                // Force columns to recalculate their widths by toggling the width property
                foreach (var column in Columns)
                {
                    if (column.Width.IsAuto)
                    {
                        // Temporarily set to a fixed width and back to Auto to force recalculation
                        var currentActualWidth = column.ActualWidth;
                        column.Width = new DataGridLength(currentActualWidth);
                        column.Width = DataGridLength.Auto;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating column sizes: {ex.Message}");
            }
        }


        #endregion
    }
}
