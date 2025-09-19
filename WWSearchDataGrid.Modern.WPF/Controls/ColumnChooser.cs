using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        /// <summary>
        /// Gets or sets the source data grid
        /// </summary>
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

        /// <summary>
        /// Gets or sets the style to apply to the window
        /// </summary>
        public Style WindowStyle
        {
            get => (Style)GetValue(WindowStyleProperty);
            set => SetValue(WindowStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the title of the window
        /// </summary>
        public string WindowTitle
        {
            get => (string)GetValue(WindowTitleProperty);
            set => SetValue(WindowTitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the window
        /// </summary>
        public double WindowWidth
        {
            get => (double)GetValue(WindowWidthProperty);
            set => SetValue(WindowWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of the window
        /// </summary>
        public double WindowHeight
        {
            get => (double)GetValue(WindowHeightProperty);
            set => SetValue(WindowHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum width of the window
        /// </summary>
        public double WindowMinWidth
        {
            get => (double)GetValue(WindowMinWidthProperty);
            set => SetValue(WindowMinWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum height of the window
        /// </summary>
        public double WindowMinHeight
        {
            get => (double)GetValue(WindowMinHeightProperty);
            set => SetValue(WindowMinHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the positioning mode for the window
        /// </summary>
        public ColumnChooserPositionMode WindowPositionMode
        {
            get => (ColumnChooserPositionMode)GetValue(WindowPositionModeProperty);
            set => SetValue(WindowPositionModeProperty, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to close the editor window
        /// </summary>
        public ICommand CloseCommand => new RelayCommand(_ => CloseWindow());

        #endregion

        #region Events

        /// <summary>
        /// Event raised when a column visibility changes
        /// </summary>
        public event EventHandler<ColumnVisibilityChangedEventArgs> ColumnVisibilityChanged;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ColumnChooser class
        /// </summary>
        public ColumnChooser()
        {
            DefaultStyleKey = typeof(ColumnChooser);
            Columns = new ObservableCollection<ColumnVisibilityInfo>();
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

        /// <summary>
        /// Closes the column chooser window
        /// </summary>
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

            // Apply custom window style if provided
            if (WindowStyle != null)
            {
                _parentWindow.Style = WindowStyle;
            }
            else
            {
                // Try to find a default style for ColumnChooserWindow
                var defaultStyle = TryFindResource(typeof(Window), "ColumnChooserWindowStyle") as Style;
                if (defaultStyle != null)
                {
                    _parentWindow.Style = defaultStyle;
                }
            }

            // Position the window based on the positioning mode
            PositionWindow(ownerWindow);

            _parentWindow.Closed += OnWindowClosed;
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

            // Ensure the window is within screen bounds
            EnsureWindowInBounds();
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

                // Try application resources
                if (Application.Current?.Resources.Contains(resourceKey) == true)
                    return Application.Current.Resources[resourceKey];

                // Try implicit style lookup
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
            _parentWindow = null;
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
                    DisplayName = column.Header?.ToString() ?? "Unknown Column",
                    IsVisible = column.Visibility == Visibility.Visible
                };

                // Subscribe to property changed to detect visibility changes
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
                // Update the actual column visibility immediately
                if (columnInfo.Column != null)
                {
                    columnInfo.Column.Visibility = columnInfo.IsVisible ? Visibility.Visible : Visibility.Collapsed;

                    // Raise the event
                    ColumnVisibilityChanged?.Invoke(this, new ColumnVisibilityChangedEventArgs(columnInfo));
                }
            }
        }

        /// <summary>
        /// Toggles the visibility of a column
        /// </summary>
        private void ToggleColumnVisibility(ColumnVisibilityInfo columnInfo)
        {
            if (columnInfo != null)
            {
                columnInfo.IsVisible = !columnInfo.IsVisible;
            }
        }

        /// <summary>
        /// Closes the window
        /// </summary>
        private void CloseWindow()
        {
            _parentWindow?.Close();
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

        /// <summary>
        /// Gets or sets the associated DataGrid column
        /// </summary>
        public DataGridColumn Column { get; set; }

        /// <summary>
        /// Gets or sets the display name of the column
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets whether the column is visible
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(value, ref _isVisible, nameof(IsVisible));
        }
    }

    /// <summary>
    /// Event arguments for column visibility changed events
    /// </summary>
    public class ColumnVisibilityChangedEventArgs : EventArgs
    {
        public ColumnVisibilityInfo ColumnInfo { get; }

        public ColumnVisibilityChangedEventArgs(ColumnVisibilityInfo columnInfo)
        {
            ColumnInfo = columnInfo;
        }
    }

    #endregion
}
