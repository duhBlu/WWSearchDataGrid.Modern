using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    public enum ColumnChooserPositionMode
    {
        BottomRight,
        BottomLeft,
        TopRight,
        TopLeft,
        Center,
        CenterScreen
    }

    /// <summary>
    /// ColumnChooser provides a non-modal window for managing column visibility
    /// </summary>
    public class ColumnChooser : Control
    {

        #region Fields

        private Window _parentWindow;
        private bool _isConfinedToGrid;
        private Window _ownerWindow;
        private Point _lastOwnerPosition;
        private bool _isDragging;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty SourceDataGridProperty =
            DependencyProperty.Register("SourceDataGrid", typeof(SearchDataGrid), typeof(ColumnChooser),
                new PropertyMetadata(null, OnSourceDataGridChanged));

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register("Columns", typeof(ObservableCollection<ColumnVisibilityInfo>), typeof(ColumnChooser),
                new PropertyMetadata(null));

        public static readonly DependencyProperty WindowStyleProperty =
            DependencyProperty.Register("WindowStyle", typeof(Style), typeof(ColumnChooser),
                new PropertyMetadata(null));

        public static readonly DependencyProperty WindowTitleProperty =
            DependencyProperty.Register("WindowTitle", typeof(string), typeof(ColumnChooser),
                new PropertyMetadata("Column Chooser"));

        public static readonly DependencyProperty WindowWidthProperty =
            DependencyProperty.Register("WindowWidth", typeof(double), typeof(ColumnChooser),
                new PropertyMetadata(300.0));

        public static readonly DependencyProperty WindowHeightProperty =
            DependencyProperty.Register("WindowHeight", typeof(double), typeof(ColumnChooser),
                new PropertyMetadata(400.0));

        public static readonly DependencyProperty WindowMinWidthProperty =
            DependencyProperty.Register("WindowMinWidth", typeof(double), typeof(ColumnChooser),
                new PropertyMetadata(250.0));

        public static readonly DependencyProperty WindowMinHeightProperty =
            DependencyProperty.Register("WindowMinHeight", typeof(double), typeof(ColumnChooser),
                new PropertyMetadata(300.0));

        public static readonly DependencyProperty WindowPositionModeProperty =
            DependencyProperty.Register("WindowPositionMode", typeof(ColumnChooserPositionMode), typeof(ColumnChooser),
                new PropertyMetadata(ColumnChooserPositionMode.BottomRight));

        #endregion

        #region Properties

        public SearchDataGrid SourceDataGrid
        {
            get => (SearchDataGrid)GetValue(SourceDataGridProperty);
            set => SetValue(SourceDataGridProperty, value);
        }

        /// <summary>
        /// Gets or sets the collection of columns with visibility information
        /// </summary>
        public ObservableCollection<ColumnVisibilityInfo> Columns
        {
            get => (ObservableCollection<ColumnVisibilityInfo>)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public Style WindowStyle
        {
            get => (Style)GetValue(WindowStyleProperty);
            set => SetValue(WindowStyleProperty, value);
        }

        public string WindowTitle
        {
            get => (string)GetValue(WindowTitleProperty);
            set => SetValue(WindowTitleProperty, value);
        }

        public double WindowWidth
        {
            get => (double)GetValue(WindowWidthProperty);
            set => SetValue(WindowWidthProperty, value);
        }

        public double WindowHeight
        {
            get => (double)GetValue(WindowHeightProperty);
            set => SetValue(WindowHeightProperty, value);
        }

        public double WindowMinWidth
        {
            get => (double)GetValue(WindowMinWidthProperty);
            set => SetValue(WindowMinWidthProperty, value);
        }

        public double WindowMinHeight
        {
            get => (double)GetValue(WindowMinHeightProperty);
            set => SetValue(WindowMinHeightProperty, value);
        }

        public ColumnChooserPositionMode WindowPositionMode
        {
            get => (ColumnChooserPositionMode)GetValue(WindowPositionModeProperty);
            set => SetValue(WindowPositionModeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the column chooser window is confined to the grid's bounds
        /// </summary>
        public bool IsConfinedToGrid
        {
            get => _isConfinedToGrid;
            set
            {
                _isConfinedToGrid = value;
                if (_parentWindow != null)
                {
                    UpdateWindowConfinement();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand CloseCommand => new RelayCommand(_ => CloseWindow());

        public ICommand SelectColumnCommand => new RelayCommand<ColumnVisibilityInfo>(column => SelectColumn(column));

        internal RelayCommand MoveUpCommand => new RelayCommand(
            param => MoveColumn(param as ColumnVisibilityInfo, -1),
            param => CanMoveColumn(param as ColumnVisibilityInfo, -1));

        internal RelayCommand MoveDownCommand => new RelayCommand(
            param => MoveColumn(param as ColumnVisibilityInfo, 1),
            param => CanMoveColumn(param as ColumnVisibilityInfo, 1));

        #endregion

        #region Constructors

        public ColumnChooser()
        {
            DefaultStyleKey = typeof(ColumnChooser);
            Columns = new ObservableCollection<ColumnVisibilityInfo>();

            // Subscribe to column display name changes
            GridColumn.ColumnDisplayNameChanged += OnColumnDisplayNameChanged;

            // Clean up subscription when this control is unloaded
            Unloaded += (s, e) => GridColumn.ColumnDisplayNameChanged -= OnColumnDisplayNameChanged;

            // Subscribe to column visibility changes to update select all state
            Columns.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (ColumnVisibilityInfo item in e.NewItems)
                    {
                        item.PropertyChanged += OnColumnVisibilityChanged;
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (ColumnVisibilityInfo item in e.OldItems)
                    {
                        item.PropertyChanged -= OnColumnVisibilityChanged;
                    }
                }
            };
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Wire up Select All checkbox
            // Use Click event only (not Checked/Unchecked) - same pattern as column header select all
            if (GetTemplateChild("PART_SelectAllCheckBox") is CheckBox selectAllCheckBox)
            {
                selectAllCheckBox.Click += OnSelectAllClick;
                selectAllCheckBox.Loaded += (s, e) => UpdateSelectAllState();
            }

            // Wire up ReorderableListBox events first so we can bind to it
            ReorderableListBox columnListBox = null;
            if (GetTemplateChild("PART_ColumnList") is ReorderableListBox columnList)
            {
                columnListBox = columnList;
                columnList.ItemReordered += OnColumnListItemReordered;
                columnList.SelectionChanged += OnColumnListSelectionChanged;
            }

            // Wire up Move Up/Down buttons with CommandParameter binding to ListBox SelectedItem
            if (GetTemplateChild("PART_MoveUpButton") is Button moveUpButton && columnListBox != null)
            {
                moveUpButton.Command = MoveUpCommand;
                // Bind CommandParameter to the ListBox's SelectedItem
                var binding = new System.Windows.Data.Binding("SelectedItem")
                {
                    Source = columnListBox,
                    Mode = System.Windows.Data.BindingMode.OneWay
                };
                moveUpButton.SetBinding(Button.CommandParameterProperty, binding);
            }

            if (GetTemplateChild("PART_MoveDownButton") is Button moveDownButton && columnListBox != null)
            {
                moveDownButton.Command = MoveDownCommand;
                // Bind CommandParameter to the ListBox's SelectedItem
                var binding = new System.Windows.Data.Binding("SelectedItem")
                {
                    Source = columnListBox,
                    Mode = System.Windows.Data.BindingMode.OneWay
                };
                moveDownButton.SetBinding(Button.CommandParameterProperty, binding);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the column chooser as a non-modal window
        /// </summary>
        public void Show()
        {
            if (_parentWindow != null && _parentWindow.IsVisible)
            {
                _parentWindow.Activate();
                return;
            }

            CreateWindow();
            _parentWindow.Show();
        }

        public void Close()
        {
            _parentWindow?.Close();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates and configures the parent window
        /// </summary>
        private void CreateWindow()
        {
            var ownerWindow = GetOwnerWindow();
            _ownerWindow = ownerWindow;

            _parentWindow = new Window
            {
                Title = WindowTitle,
                Content = this,
                Width = WindowWidth,
                Height = WindowHeight,
                MinWidth = WindowMinWidth,
                MinHeight = WindowMinHeight,
                ResizeMode = ResizeMode.CanResize,
                ShowInTaskbar = false,
                WindowStyle = System.Windows.WindowStyle.ToolWindow,
                Owner = ownerWindow,
            };

            // Track the initial owner window position
            if (_ownerWindow != null)
            {
                _lastOwnerPosition = new Point(_ownerWindow.Left, _ownerWindow.Top);
            }

            // Apply custom window style if provided
            if (WindowStyle != null)
            {
                _parentWindow.Style = WindowStyle;
            }
            else
            {
                // Try to find a default style for ColumnChooserWindow
                var defaultStyle = TryFindResource(typeof(Window), "GenericColumnChooserWindowStyle") as Style;
                if (defaultStyle != null)
                {
                    _parentWindow.Style = defaultStyle;
                }
            }

            PositionWindow(ownerWindow);

            _parentWindow.Closed += OnWindowClosed;

            // Set up confinement if enabled
            if (_isConfinedToGrid)
            {
                UpdateWindowConfinement();
            }
        }

        /// <summary>
        /// Positions the window based on the positioning mode
        /// </summary>
        private void PositionWindow(Window ownerWindow)
        {
            if (ownerWindow == null)
            {
                _parentWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            _parentWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            // If we have a SourceDataGrid, position relative to it instead of the owner window
            if (SourceDataGrid != null)
            {
                PositionRelativeToGrid();
            }
            else
            {
                // Fallback to owner window positioning
                switch (WindowPositionMode)
                {
                    case ColumnChooserPositionMode.BottomRight:
                        _parentWindow.Left = ownerWindow.Left + ownerWindow.Width - _parentWindow.Width - 20;
                        _parentWindow.Top = ownerWindow.Top + ownerWindow.Height - _parentWindow.Height - 60;
                        break;

                    case ColumnChooserPositionMode.BottomLeft:
                        _parentWindow.Left = ownerWindow.Left + 20;
                        _parentWindow.Top = ownerWindow.Top + ownerWindow.Height - _parentWindow.Height - 60;
                        break;

                    case ColumnChooserPositionMode.TopRight:
                        _parentWindow.Left = ownerWindow.Left + ownerWindow.Width - _parentWindow.Width - 20;
                        _parentWindow.Top = ownerWindow.Top + 60;
                        break;

                    case ColumnChooserPositionMode.TopLeft:
                        _parentWindow.Left = ownerWindow.Left + 20;
                        _parentWindow.Top = ownerWindow.Top + 60;
                        break;

                    case ColumnChooserPositionMode.Center:
                        _parentWindow.Left = ownerWindow.Left + (ownerWindow.Width - _parentWindow.Width) / 2;
                        _parentWindow.Top = ownerWindow.Top + (ownerWindow.Height - _parentWindow.Height) / 2;
                        break;

                    case ColumnChooserPositionMode.CenterScreen:
                        _parentWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        break;
                }

                EnsureWindowInBounds();
            }
        }

        /// <summary>
        /// Positions the window relative to the DataGrid viewport
        /// </summary>
        private void PositionRelativeToGrid()
        {
            try
            {
                // Get the DataGrid bounds in screen coordinates
                var gridTopLeft = SourceDataGrid.PointToScreen(new Point(0, 0));
                var gridBottomRight = SourceDataGrid.PointToScreen(new Point(SourceDataGrid.ActualWidth, SourceDataGrid.ActualHeight));

                double gridWidth = gridBottomRight.X - gridTopLeft.X;
                double gridHeight = gridBottomRight.Y - gridTopLeft.Y;

                // Position in bottom-left corner with some padding
                double padding = 10;
                _parentWindow.Left = gridTopLeft.X + padding;
                _parentWindow.Top = gridBottomRight.Y - _parentWindow.Height - padding;

                // If confinement is enabled, ensure it's within bounds
                if (_isConfinedToGrid)
                {
                    // Ensure the window fits within grid bounds
                    if (_parentWindow.Width > gridWidth - (2 * padding))
                    {
                        _parentWindow.Width = gridWidth - (2 * padding);
                    }
                    if (_parentWindow.Height > gridHeight - (2 * padding))
                    {
                        _parentWindow.Height = gridHeight - (2 * padding);
                    }

                    // Ensure position is within bounds
                    if (_parentWindow.Left + _parentWindow.Width > gridBottomRight.X - padding)
                    {
                        _parentWindow.Left = gridBottomRight.X - _parentWindow.Width - padding;
                    }
                    if (_parentWindow.Top < gridTopLeft.Y + padding)
                    {
                        _parentWindow.Top = gridTopLeft.Y + padding;
                    }
                }
                else
                {
                    // When not confined, still ensure it's visible on screen
                    EnsureWindowInBounds();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error positioning relative to grid: {ex.Message}");
                // Fallback to center screen if positioning fails
                _parentWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        /// <summary>
        /// Ensures the window is positioned within screen bounds
        /// </summary>
        private void EnsureWindowInBounds()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            if (_parentWindow.Left + _parentWindow.Width > screenWidth)
                _parentWindow.Left = screenWidth - _parentWindow.Width - 10;

            if (_parentWindow.Top + _parentWindow.Height > screenHeight)
                _parentWindow.Top = screenHeight - _parentWindow.Height - 10;

            if (_parentWindow.Left < 0)
                _parentWindow.Left = 10;

            if (_parentWindow.Top < 0)
                _parentWindow.Top = 10;
        }

        /// <summary>
        /// Tries to find a resource in the current application
        /// </summary>
        private object TryFindResource(Type targetType, string resourceKey)
        {
            try
            {
                // Try to find in the current element's resources first
                if (Resources.Contains(resourceKey))
                    return Resources[resourceKey];

                if (Application.Current?.Resources.Contains(resourceKey) == true)
                    return Application.Current.Resources[resourceKey];

                if (Application.Current?.Resources.Contains(targetType) == true)
                    return Application.Current.Resources[targetType];

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the owner window for the column chooser
        /// </summary>
        private Window GetOwnerWindow()
        {
            if (SourceDataGrid != null)
            {
                return Window.GetWindow(SourceDataGrid);
            }

            return Application.Current?.MainWindow;
        }

        /// <summary>
        /// Handles window closed event
        /// </summary>
        private void OnWindowClosed(object sender, EventArgs e)
        {
            _parentWindow.Closed -= OnWindowClosed;
            CleanupConfinement();
            _parentWindow = null;
            _ownerWindow = null;
        }

        /// <summary>
        /// Handles changes to the source data grid
        /// </summary>
        private static void OnSourceDataGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnChooser editor)
            {
                editor.RefreshColumns();
            }
        }

        /// <summary>
        /// Refreshes the columns collection from the source data grid
        /// </summary>
        private void RefreshColumns()
        {
            Columns.Clear();

            if (SourceDataGrid?.Columns == null) return;

            foreach (DataGridColumn column in SourceDataGrid.Columns)
            {
                var columnInfo = new ColumnVisibilityInfo
                {
                    Column = column,
                    DisplayName = GridColumn.GetEffectiveColumnDisplayName(column) ?? "Unknown Column",
                    IsVisible = column.Visibility == Visibility.Visible
                };

                columnInfo.PropertyChanged += OnColumnInfoPropertyChanged;

                Columns.Add(columnInfo);
            }
        }

        /// <summary>
        /// Handles property changes on column info objects
        /// </summary>
        private void OnColumnInfoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ColumnVisibilityInfo.IsVisible) && sender is ColumnVisibilityInfo columnInfo)
            {
                if (columnInfo.Column != null)
                {
                    columnInfo.Column.Visibility = columnInfo.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Handles changes to column display names
        /// </summary>
        private void OnColumnDisplayNameChanged(object sender, ColumnDisplayNameChangedEventArgs e)
        {
            if (e?.Column == null)
                return;

            // Find the ColumnVisibilityInfo for this column and update its DisplayName
            var columnInfo = Columns.FirstOrDefault(c => c.Column == e.Column);
            if (columnInfo != null)
            {
                columnInfo.DisplayName = e.NewValue ?? GridColumn.GetEffectiveColumnDisplayName(e.Column) ?? "Unknown Column";
            }
        }

        private void CloseWindow()
        {
            _parentWindow?.Close();
        }

        /// <summary>
        /// Updates the window confinement behavior
        /// </summary>
        private void UpdateWindowConfinement()
        {
            if (_parentWindow == null)
                return;

            if (_isConfinedToGrid)
            {
                // Subscribe to SourceInitialized to intercept window dragging
                if (!_parentWindow.IsInitialized)
                {
                    _parentWindow.SourceInitialized += OnWindowSourceInitialized;
                }
                else
                {
                    SetupWindowHook();
                }

                // Subscribe to owner window LocationChanged to track parent movement
                if (_ownerWindow != null)
                {
                    _ownerWindow.LocationChanged += OnOwnerLocationChanged;
                }

                // Set up a timer to check grid size changes
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
                timer.Tick += OnCheckGridBounds;
                timer.Start();
                _parentWindow.Tag = timer; // Store timer for cleanup
            }
            else
            {
                // Clean up event handlers
                CleanupConfinement();
            }
        }

        /// <summary>
        /// Handles when the window source is initialized
        /// </summary>
        private void OnWindowSourceInitialized(object sender, EventArgs e)
        {
            _parentWindow.SourceInitialized -= OnWindowSourceInitialized;
            SetupWindowHook();
        }

        /// <summary>
        /// Sets up the window procedure hook for preventing dragging outside bounds
        /// </summary>
        private void SetupWindowHook()
        {
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(_parentWindow);
                var source = System.Windows.Interop.HwndSource.FromHwnd(helper.Handle);
                source?.AddHook(WndProc);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up window hook: {ex.Message}");
            }
        }

        /// <summary>
        /// Win32 constants for window messages
        /// </summary>
        private const int WM_MOVING = 0x0216;
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int WM_ENTERSIZEMOVE = 0x0231;
        private const int WM_EXITSIZEMOVE = 0x0232;
        private const int WM_SIZING = 0x0214;
        private const int WM_GETMINMAXINFO = 0x0024;

        // WindowPos flags
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOZORDER = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        /// <summary>
        /// Window procedure hook to intercept drag messages
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (!_isConfinedToGrid || SourceDataGrid == null || _parentWindow == null)
                return IntPtr.Zero;

            try
            {
                switch (msg)
                {
                    case WM_ENTERSIZEMOVE:
                        _isDragging = true;
                        break;

                    case WM_EXITSIZEMOVE:
                        _isDragging = false;
                        // Final constraint check after dragging ends
                        ConstrainWindowToBounds();
                        break;

                    case WM_GETMINMAXINFO:
                        // Prevent maximize and set max size to grid bounds
                        var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

                        var gridTopLeft = SourceDataGrid.PointToScreen(new Point(0, 0));
                        var gridBottomRight = SourceDataGrid.PointToScreen(new Point(SourceDataGrid.ActualWidth, SourceDataGrid.ActualHeight));

                        int maxWidth = (int)(gridBottomRight.X - gridTopLeft.X);
                        int maxHeight = (int)(gridBottomRight.Y - gridTopLeft.Y);

                        // Set maximum tracking size (prevents resize beyond grid)
                        mmi.ptMaxTrackSize.x = maxWidth;
                        mmi.ptMaxTrackSize.y = maxHeight;

                        // Set maximum size (prevents maximize)
                        mmi.ptMaxSize.x = maxWidth;
                        mmi.ptMaxSize.y = maxHeight;

                        // Override min size if grid is smaller
                        int minWidth = Math.Min((int)_parentWindow.MinWidth, maxWidth);
                        int minHeight = Math.Min((int)_parentWindow.MinHeight, maxHeight);
                        mmi.ptMinTrackSize.x = minWidth;
                        mmi.ptMinTrackSize.y = minHeight;

                        Marshal.StructureToPtr(mmi, lParam, true);
                        handled = true;
                        break;

                    case WM_WINDOWPOSCHANGING:
                        if (_isDragging)
                        {
                            var windowPos = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                            // Only process if position is changing (not just size/z-order)
                            if ((windowPos.flags & SWP_NOMOVE) == 0)
                            {
                                var gridBounds = GetGridBounds();

                                int windowWidth = windowPos.cx > 0 ? windowPos.cx : (int)_parentWindow.ActualWidth;
                                int windowHeight = windowPos.cy > 0 ? windowPos.cy : (int)_parentWindow.ActualHeight;

                                // Constrain position
                                int newX = Math.Max(gridBounds.Left, Math.Min(windowPos.x, gridBounds.Right - windowWidth));
                                int newY = Math.Max(gridBounds.Top, Math.Min(windowPos.y, gridBounds.Bottom - windowHeight));

                                // Only update if changed to avoid feedback loop
                                if (newX != windowPos.x || newY != windowPos.y)
                                {
                                    windowPos.x = newX;
                                    windowPos.y = newY;
                                    Marshal.StructureToPtr(windowPos, lParam, true);
                                }
                            }
                        }
                        break;

                    case WM_SIZING:
                        // Constrain window size to grid bounds
                        var rect = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
                        ConstrainSizeToBounds(ref rect);
                        Marshal.StructureToPtr(rect, lParam, true);
                        handled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in WndProc: {ex.Message}");
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Gets the grid bounds as a RECT structure
        /// </summary>
        private RECT GetGridBounds()
        {
            var gridTopLeft = SourceDataGrid.PointToScreen(new Point(0, 0));
            var gridBottomRight = SourceDataGrid.PointToScreen(new Point(SourceDataGrid.ActualWidth, SourceDataGrid.ActualHeight));

            return new RECT
            {
                Left = (int)gridTopLeft.X,
                Top = (int)gridTopLeft.Y,
                Right = (int)gridBottomRight.X,
                Bottom = (int)gridBottomRight.Y
            };
        }

        /// <summary>
        /// Constrains the window size to fit within grid bounds
        /// </summary>
        private void ConstrainSizeToBounds(ref RECT rect)
        {
            try
            {
                var gridBounds = GetGridBounds();
                int maxWidth = gridBounds.Right - gridBounds.Left;
                int maxHeight = gridBounds.Bottom - gridBounds.Top;

                int windowWidth = rect.Right - rect.Left;
                int windowHeight = rect.Bottom - rect.Top;

                // Constrain width
                if (windowWidth > maxWidth)
                {
                    rect.Right = rect.Left + maxWidth;
                }

                // Constrain height
                if (windowHeight > maxHeight)
                {
                    rect.Bottom = rect.Top + maxHeight;
                }

                // Ensure the resized window doesn't exceed grid bounds
                if (rect.Right > gridBounds.Right)
                {
                    rect.Right = gridBounds.Right;
                }
                if (rect.Bottom > gridBounds.Bottom)
                {
                    rect.Bottom = gridBounds.Bottom;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error constraining size: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles owner window location changes to move the column chooser along with it
        /// </summary>
        private void OnOwnerLocationChanged(object sender, EventArgs e)
        {
            if (!_isConfinedToGrid || _ownerWindow == null || _parentWindow == null)
                return;

            try
            {
                // Calculate the delta movement
                double deltaX = _ownerWindow.Left - _lastOwnerPosition.X;
                double deltaY = _ownerWindow.Top - _lastOwnerPosition.Y;

                // Move the column chooser by the same delta
                if (Math.Abs(deltaX) > 0.1 || Math.Abs(deltaY) > 0.1)
                {
                    _parentWindow.Left += deltaX;
                    _parentWindow.Top += deltaY;

                    // Ensure it stays within bounds after movement
                    ConstrainWindowToBounds();
                }

                // Update the last position
                _lastOwnerPosition = new Point(_ownerWindow.Left, _ownerWindow.Top);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling owner location change: {ex.Message}");
            }
        }

        /// <summary>
        /// Periodically checks if the grid bounds have changed
        /// </summary>
        private void OnCheckGridBounds(object sender, EventArgs e)
        {
            if (!_isConfinedToGrid || SourceDataGrid == null || _parentWindow == null)
                return;

            try
            {
                // Constrain window if grid size changed
                ConstrainWindowToBounds();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking grid bounds: {ex.Message}");
            }
        }

        /// <summary>
        /// Constrains the window position to stay within grid bounds
        /// </summary>
        private void ConstrainWindowToBounds()
        {
            if (SourceDataGrid == null || _parentWindow == null)
                return;

            try
            {
                var gridTopLeft = SourceDataGrid.PointToScreen(new Point(0, 0));
                var gridBottomRight = SourceDataGrid.PointToScreen(new Point(SourceDataGrid.ActualWidth, SourceDataGrid.ActualHeight));

                double left = _parentWindow.Left;
                double top = _parentWindow.Top;
                double right = left + _parentWindow.ActualWidth;
                double bottom = top + _parentWindow.ActualHeight;

                bool changed = false;

                // Constrain to bounds
                if (left < gridTopLeft.X)
                {
                    left = gridTopLeft.X;
                    changed = true;
                }
                if (right > gridBottomRight.X)
                {
                    left = gridBottomRight.X - _parentWindow.ActualWidth;
                    changed = true;
                }
                if (top < gridTopLeft.Y)
                {
                    top = gridTopLeft.Y;
                    changed = true;
                }
                if (bottom > gridBottomRight.Y)
                {
                    top = gridBottomRight.Y - _parentWindow.ActualHeight;
                    changed = true;
                }

                if (changed)
                {
                    _parentWindow.Left = left;
                    _parentWindow.Top = top;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error constraining window: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles select all checkbox click event
        /// Uses the same pattern as column header select all checkbox
        /// </summary>
        private void OnSelectAllClick(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkbox)
                return;

            // Calculate current state
            var currentState = CalculateSelectAllState();

            // Determine new value (same logic as SearchDataGrid.ToggleSelectAllColumn):
            // - If all true (currentState == true), set all to false
            // - If all false (currentState == false), set all to true
            // - If mixed (currentState == null), set all to true
            bool newValue = currentState != true;

            // Apply the new value to all columns
            foreach (var column in Columns)
            {
                column.IsVisible = newValue;
            }

            // Update the checkbox state to reflect the new data state
            checkbox.IsChecked = CalculateSelectAllState();
        }

        /// <summary>
        /// Handles column visibility property changes to update select all state
        /// </summary>
        private void OnColumnVisibilityChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ColumnVisibilityInfo.IsVisible))
            {
                UpdateSelectAllState();
            }
        }

        /// <summary>
        /// Handles selection changes in the ReorderableListBox
        /// </summary>
        private void OnColumnListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update IsSelected property to match ListBox selection
            if (sender is ReorderableListBox listBox)
            {
                foreach (var column in Columns)
                {
                    column.IsSelected = (column == listBox.SelectedItem);
                }

                // Note: CommandParameter binding will automatically trigger CanExecute re-evaluation
                // when SelectedItem changes, so no need to manually invalidate
            }
        }

        /// <summary>
        /// Handles drag-drop reordering of columns in the ReorderableListBox
        /// </summary>
        private void OnColumnListItemReordered(object sender, ItemReorderedEventArgs e)
        {
            if (e.Item is not ColumnVisibilityInfo columnInfo || SourceDataGrid == null)
                return;

            int oldIndex = e.OldIndex;
            int newIndex = e.NewIndex;

            if (columnInfo.Column == null || oldIndex < 0 || newIndex < 0)
                return;

            if (oldIndex == newIndex)
                return;

            try
            {
                // 1. Move in our Columns ObservableCollection to update the visual display
                if (oldIndex < Columns.Count && newIndex < Columns.Count)
                {
                    Columns.Move(oldIndex, newIndex);
                }

                // 2. Move in the DataGrid columns collection to update the actual grid
                int gridOldIndex = SourceDataGrid.Columns.IndexOf(columnInfo.Column);

                if (gridOldIndex >= 0 && newIndex < SourceDataGrid.Columns.Count)
                {
                    SourceDataGrid.Columns.Move(gridOldIndex, newIndex);

                    // Verify the move
                    int verifyIndex = SourceDataGrid.Columns.IndexOf(columnInfo.Column);
                    
                    // IMPORTANT: Update DisplayIndex to match the new position
                    // DataGrid uses DisplayIndex for visual rendering, not collection index!
                    columnInfo.Column.DisplayIndex = newIndex;

                    // Force layout update
                    SourceDataGrid.UpdateLayout();

                    MoveUpCommand.RaiseCanExecuteChanged();
                    MoveDownCommand.RaiseCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ItemReordered] ERROR: {ex.Message}");
                Debug.WriteLine($"[ItemReordered] Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Calculates the select all checkbox state based on column visibility
        /// Returns true if all visible, false if all hidden, null if mixed
        /// </summary>
        private bool? CalculateSelectAllState()
        {
            if (Columns.Count == 0)
                return false;

            int visibleCount = Columns.Count(c => c.IsVisible);

            if (visibleCount == 0)
                return false;
            else if (visibleCount == Columns.Count)
                return true;
            else
                return null; // Indeterminate (mixed state)
        }

        /// <summary>
        /// Updates the select all checkbox state based on column visibility
        /// </summary>
        private void UpdateSelectAllState()
        {
            if (GetTemplateChild("PART_SelectAllCheckBox") is CheckBox selectAllCheckBox)
            {
                selectAllCheckBox.IsChecked = CalculateSelectAllState();
            }
        }

        /// <summary>
        /// Selects a column in the list
        /// </summary>
        private void SelectColumn(ColumnVisibilityInfo columnInfo)
        {
            if (columnInfo == null)
                return;

            // Deselect all others
            foreach (var col in Columns)
            {
                col.IsSelected = false;
            }

            // Select this one
            columnInfo.IsSelected = true;
        }

        /// <summary>
        /// Moves the specified column up or down in the display order
        /// </summary>
        private void MoveColumn(ColumnVisibilityInfo columnInfo, int direction)
        {
            if (columnInfo?.Column == null || SourceDataGrid == null)
            {
                return;
            }

            try
            {
                // Get current indices
                int displayIndex = Columns.IndexOf(columnInfo);
                int gridIndex = SourceDataGrid.Columns.IndexOf(columnInfo.Column);


                if (displayIndex < 0 || gridIndex < 0)
                {
                    return;
                }

                // Calculate new indices
                int newDisplayIndex = displayIndex + direction;
                int newGridIndex = gridIndex + direction;

                // Validate new indices
                if (newDisplayIndex >= 0 && newDisplayIndex < Columns.Count &&
                    newGridIndex >= 0 && newGridIndex < SourceDataGrid.Columns.Count)
                {
                    // Move in our display list first
                    Columns.Move(displayIndex, newDisplayIndex);
                    SourceDataGrid.Columns.Move(gridIndex, newGridIndex);

                    // Verify the move
                    int verifyIndex = SourceDataGrid.Columns.IndexOf(columnInfo.Column);

                    // IMPORTANT: Update DisplayIndex to match the new position
                    // DataGrid uses DisplayIndex for visual rendering, not collection index!
                    columnInfo.Column.DisplayIndex = newGridIndex;

                    // Force layout update
                    SourceDataGrid.UpdateLayout();

                    MoveUpCommand.RaiseCanExecuteChanged();
                    MoveDownCommand.RaiseCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MoveColumn] Error moving column: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines if the specified column can be moved in the specified direction
        /// </summary>
        private bool CanMoveColumn(ColumnVisibilityInfo columnInfo, int direction)
        {
            if (SourceDataGrid == null)
                return false;

            // Check if reordering is allowed
            if (!SourceDataGrid.CanUserReorderColumns)
                return false;

            if (columnInfo == null)
                return false;

            int currentIndex = Columns.IndexOf(columnInfo);
            if (currentIndex < 0)
                return false;

            int newIndex = currentIndex + direction;
            return newIndex >= 0 && newIndex < Columns.Count;
        }

        /// <summary>
        /// Cleans up confinement-related event handlers and resources
        /// </summary>
        private void CleanupConfinement()
        {
            try
            {
                if (_ownerWindow != null)
                {
                    _ownerWindow.LocationChanged -= OnOwnerLocationChanged;
                }

                if (_parentWindow != null && _parentWindow.Tag is System.Windows.Threading.DispatcherTimer timer)
                {
                    timer.Stop();
                    timer.Tick -= OnCheckGridBounds;
                    _parentWindow.Tag = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning up confinement: {ex.Message}");
            }
        }
    }

    #endregion

    #region Supporting Classes

    /// <summary>
    /// Represents column visibility information
    /// </summary>
    public class ColumnVisibilityInfo : ObservableObject
    {
        private bool _isVisible;
        private string _displayName;
        private bool _isSelected;

        public DataGridColumn Column { get; set; }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(value, ref _displayName, nameof(DisplayName));
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(value, ref _isVisible, nameof(IsVisible));
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(value, ref _isSelected, nameof(IsSelected));
        }
    }

    #endregion
}
