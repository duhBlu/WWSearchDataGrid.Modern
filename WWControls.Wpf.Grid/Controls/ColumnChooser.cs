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
using System.Windows.Threading;
using WWControls.Core;
using WWControls.Wpf.Commands;

namespace WWControls.Wpf.Grids
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
        private DispatcherOperation _pendingScrollRefresh;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty SourceDataGridProperty =
            DependencyProperty.Register("SourceDataGrid", typeof(SearchDataGrid), typeof(ColumnChooser),
                new PropertyMetadata(null, OnSourceDataGridChanged));

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register("Columns", typeof(ObservableCollection<ColumnVisibilityInfo>), typeof(ColumnChooser),
                new PropertyMetadata(null));

        /// <summary>
        /// Backing collection for the Left-pinned section listbox. Reorder operations
        /// happen within this collection; cross-section drags are impossible because
        /// each section has its own <see cref="WWListBox"/>.
        /// </summary>
        public static readonly DependencyProperty LeftFixedColumnsProperty =
            DependencyProperty.Register(nameof(LeftFixedColumns), typeof(ObservableCollection<ColumnVisibilityInfo>), typeof(ColumnChooser),
                new PropertyMetadata(null));

        /// <summary>Backing collection for the unpinned (middle) section listbox.</summary>
        public static readonly DependencyProperty UnpinnedColumnsProperty =
            DependencyProperty.Register(nameof(UnpinnedColumns), typeof(ObservableCollection<ColumnVisibilityInfo>), typeof(ColumnChooser),
                new PropertyMetadata(null));

        /// <summary>Backing collection for the Right-pinned section listbox.</summary>
        public static readonly DependencyProperty RightFixedColumnsProperty =
            DependencyProperty.Register(nameof(RightFixedColumns), typeof(ObservableCollection<ColumnVisibilityInfo>), typeof(ColumnChooser),
                new PropertyMetadata(null));

        /// <summary>
        /// The currently selected <see cref="ColumnVisibilityInfo"/> across all three
        /// section listboxes. Updated when any section ListBox raises SelectionChanged
        /// and used as the CommandParameter for the Move Up / Move Down buttons.
        /// </summary>
        public static readonly DependencyProperty SelectedColumnProperty =
            DependencyProperty.Register(nameof(SelectedColumn), typeof(ColumnVisibilityInfo), typeof(ColumnChooser),
                new PropertyMetadata(null));

        /// <summary>
        /// True when <see cref="LeftFixedColumns"/> has more than one item — gates
        /// drag-drop on the Left section listbox. A single-item list has no
        /// meaningful reorder, so we disable drag rather than animate a no-op.
        /// </summary>
        public static readonly DependencyProperty IsLeftDragEnabledProperty =
            DependencyProperty.Register(nameof(IsLeftDragEnabled), typeof(bool), typeof(ColumnChooser),
                new PropertyMetadata(false));

        /// <summary>True when <see cref="UnpinnedColumns"/> has more than one item.</summary>
        public static readonly DependencyProperty IsUnpinnedDragEnabledProperty =
            DependencyProperty.Register(nameof(IsUnpinnedDragEnabled), typeof(bool), typeof(ColumnChooser),
                new PropertyMetadata(false));

        /// <summary>True when <see cref="RightFixedColumns"/> has more than one item.</summary>
        public static readonly DependencyProperty IsRightDragEnabledProperty =
            DependencyProperty.Register(nameof(IsRightDragEnabled), typeof(bool), typeof(ColumnChooser),
                new PropertyMetadata(false));

        /// <summary>True when <see cref="LeftFixedColumns"/> has any items. Collapses the
        /// section listbox when false so the chooser doesn't reserve empty rows.</summary>
        public static readonly DependencyProperty HasLeftFixedColumnsProperty =
            DependencyProperty.Register(nameof(HasLeftFixedColumns), typeof(bool), typeof(ColumnChooser),
                new PropertyMetadata(false));

        /// <summary>True when <see cref="UnpinnedColumns"/> has any items.</summary>
        public static readonly DependencyProperty HasUnpinnedColumnsProperty =
            DependencyProperty.Register(nameof(HasUnpinnedColumns), typeof(bool), typeof(ColumnChooser),
                new PropertyMetadata(false));

        /// <summary>True when <see cref="RightFixedColumns"/> has any items.</summary>
        public static readonly DependencyProperty HasRightFixedColumnsProperty =
            DependencyProperty.Register(nameof(HasRightFixedColumns), typeof(bool), typeof(ColumnChooser),
                new PropertyMetadata(false));

        /// <summary>
        /// Divider between the Left section and whatever follows it. Visible when
        /// Left has items AND at least one of Unpinned/Right also has items.
        /// </summary>
        public static readonly DependencyProperty IsTopDividerVisibleProperty =
            DependencyProperty.Register(nameof(IsTopDividerVisible), typeof(bool), typeof(ColumnChooser),
                new PropertyMetadata(false));

        /// <summary>
        /// Divider between the Unpinned section and the Right section. Visible when
        /// both are populated. The Left/Right-only case is handled by the top divider.
        /// </summary>
        public static readonly DependencyProperty IsBottomDividerVisibleProperty =
            DependencyProperty.Register(nameof(IsBottomDividerVisible), typeof(bool), typeof(ColumnChooser),
                new PropertyMetadata(false));

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
        /// Master flat collection of every column shown in the chooser, ordered
        /// Left-pinned → unpinned → Right-pinned. The three section collections
        /// (<see cref="LeftFixedColumns"/>, <see cref="UnpinnedColumns"/>,
        /// <see cref="RightFixedColumns"/>) are partitions of this list and are
        /// what the three section listboxes bind to. Consumers that iterate
        /// chooser state (e.g. context-menu command handlers) read from
        /// <see cref="Columns"/>.
        /// </summary>
        public ObservableCollection<ColumnVisibilityInfo> Columns
        {
            get => (ObservableCollection<ColumnVisibilityInfo>)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        /// <summary>Left-pinned section. See <see cref="LeftFixedColumnsProperty"/>.</summary>
        public ObservableCollection<ColumnVisibilityInfo> LeftFixedColumns
        {
            get => (ObservableCollection<ColumnVisibilityInfo>)GetValue(LeftFixedColumnsProperty);
            set => SetValue(LeftFixedColumnsProperty, value);
        }

        /// <summary>Unpinned section. See <see cref="UnpinnedColumnsProperty"/>.</summary>
        public ObservableCollection<ColumnVisibilityInfo> UnpinnedColumns
        {
            get => (ObservableCollection<ColumnVisibilityInfo>)GetValue(UnpinnedColumnsProperty);
            set => SetValue(UnpinnedColumnsProperty, value);
        }

        /// <summary>Right-pinned section. See <see cref="RightFixedColumnsProperty"/>.</summary>
        public ObservableCollection<ColumnVisibilityInfo> RightFixedColumns
        {
            get => (ObservableCollection<ColumnVisibilityInfo>)GetValue(RightFixedColumnsProperty);
            set => SetValue(RightFixedColumnsProperty, value);
        }

        /// <summary>The currently selected column across all three sections.</summary>
        public ColumnVisibilityInfo SelectedColumn
        {
            get => (ColumnVisibilityInfo)GetValue(SelectedColumnProperty);
            set => SetValue(SelectedColumnProperty, value);
        }

        /// <summary>Whether the Left section listbox should allow drag-drop reordering (count &gt; 1).</summary>
        public bool IsLeftDragEnabled
        {
            get => (bool)GetValue(IsLeftDragEnabledProperty);
            private set => SetValue(IsLeftDragEnabledProperty, value);
        }

        /// <summary>Whether the unpinned section listbox should allow drag-drop reordering (count &gt; 1).</summary>
        public bool IsUnpinnedDragEnabled
        {
            get => (bool)GetValue(IsUnpinnedDragEnabledProperty);
            private set => SetValue(IsUnpinnedDragEnabledProperty, value);
        }

        /// <summary>Whether the Right section listbox should allow drag-drop reordering (count &gt; 1).</summary>
        public bool IsRightDragEnabled
        {
            get => (bool)GetValue(IsRightDragEnabledProperty);
            private set => SetValue(IsRightDragEnabledProperty, value);
        }

        /// <summary>True when the Left section has any items.</summary>
        public bool HasLeftFixedColumns
        {
            get => (bool)GetValue(HasLeftFixedColumnsProperty);
            private set => SetValue(HasLeftFixedColumnsProperty, value);
        }

        /// <summary>True when the Unpinned section has any items.</summary>
        public bool HasUnpinnedColumns
        {
            get => (bool)GetValue(HasUnpinnedColumnsProperty);
            private set => SetValue(HasUnpinnedColumnsProperty, value);
        }

        /// <summary>True when the Right section has any items.</summary>
        public bool HasRightFixedColumns
        {
            get => (bool)GetValue(HasRightFixedColumnsProperty);
            private set => SetValue(HasRightFixedColumnsProperty, value);
        }

        /// <summary>Visibility of the divider between Left and the rest.</summary>
        public bool IsTopDividerVisible
        {
            get => (bool)GetValue(IsTopDividerVisibleProperty);
            private set => SetValue(IsTopDividerVisibleProperty, value);
        }

        /// <summary>Visibility of the divider between Unpinned and Right.</summary>
        public bool IsBottomDividerVisible
        {
            get => (bool)GetValue(IsBottomDividerVisibleProperty);
            private set => SetValue(IsBottomDividerVisibleProperty, value);
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

        private ICommand _closeCommand;
        public ICommand CloseCommand => _closeCommand ??= new RelayCommand(_ => CloseWindow());

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
            LeftFixedColumns = new ObservableCollection<ColumnVisibilityInfo>();
            UnpinnedColumns = new ObservableCollection<ColumnVisibilityInfo>();
            RightFixedColumns = new ObservableCollection<ColumnVisibilityInfo>();

            // Cached delegate so AddValueChanged/RemoveValueChanged see the same
            // instance — DependencyPropertyDescriptor uses identity equality.
            _onCanUserReorderColumnsChanged = (_, _) => UpdateSectionFlags();

            // Subscribe to column visibility changes to update select all state.
            // Only the master Columns list owns the PropertyChanged subscription;
            // section lists hold the same instances, so we don't double-subscribe.
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

            // Drag-enablement and section-visibility flags are derived state — recompute
            // whenever any of the three section collections changes membership.
            LeftFixedColumns.CollectionChanged += (s, e) => UpdateSectionFlags();
            UnpinnedColumns.CollectionChanged += (s, e) => UpdateSectionFlags();
            RightFixedColumns.CollectionChanged += (s, e) => UpdateSectionFlags();
        }

        /// <summary>
        /// Recomputes drag-enabled / has-section / divider-visibility flags from the
        /// three section counts and the parent grid's <see cref="DataGrid.CanUserReorderColumns"/>.
        /// Called from every <see cref="System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged"/>
        /// on the section collections, on <see cref="SourceDataGrid"/> change, and when the
        /// observed grid toggles its reorder flag.
        /// </summary>
        private void UpdateSectionFlags()
        {
            int leftCount = LeftFixedColumns.Count;
            int unpinnedCount = UnpinnedColumns.Count;
            int rightCount = RightFixedColumns.Count;

            bool hasLeft = leftCount > 0;
            bool hasUnpinned = unpinnedCount > 0;
            bool hasRight = rightCount > 0;
            bool canReorder = SourceDataGrid?.CanUserReorderColumns ?? false;

            HasLeftFixedColumns = hasLeft;
            HasUnpinnedColumns = hasUnpinned;
            HasRightFixedColumns = hasRight;

            // Drag requires both a meaningful destination (count > 1) and grid-level
            // permission. The grid flag mirrors the original single-listbox behavior.
            IsLeftDragEnabled = canReorder && leftCount > 1;
            IsUnpinnedDragEnabled = canReorder && unpinnedCount > 1;
            IsRightDragEnabled = canReorder && rightCount > 1;

            // Divider rules:
            //   Top divider — between Left and whatever follows. Shown when Left has
            //   items AND at least one downstream section also has items. Handles the
            //   Left+Right-only case (Unpinned collapsed): one divider, between them.
            //   Bottom divider — between Unpinned and Right. Shown only when both
            //   sections are populated; otherwise the top divider already covers it.
            IsTopDividerVisible = hasLeft && (hasUnpinned || hasRight);
            IsBottomDividerVisible = hasUnpinned && hasRight;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Detach any prior section listbox handlers — OnApplyTemplate can run more
            // than once if the control template is re-applied at runtime, so cleanup
            // prevents duplicate subscriptions.
            DetachSectionListBoxes();

            // Wire up Select All checkbox
            // Use Click event only (not Checked/Unchecked) - same pattern as column header select all
            if (GetTemplateChild("PART_SelectAllCheckBox") is CheckBox selectAllCheckBox)
            {
                selectAllCheckBox.Click += OnSelectAllClick;
                selectAllCheckBox.Loaded += (s, e) => UpdateSelectAllState();
            }

            // Wire up the three section listboxes — each handles its own drag-drop and
            // raises ItemReordered with section-local indices. Cross-section drags are
            // impossible by construction.
            _leftSectionListBox = GetTemplateChild("PART_LeftSectionList") as WWListBox;
            _unpinnedSectionListBox = GetTemplateChild("PART_UnpinnedSectionList") as WWListBox;
            _rightSectionListBox = GetTemplateChild("PART_RightSectionList") as WWListBox;

            foreach (var listBox in EnumerateSectionListBoxes())
            {
                listBox.ItemReordered += OnSectionListItemReordered;
                listBox.SelectionChanged += OnSectionListBoxSelectionChanged;
                // The shared column-header context menu binds against a ContextMenuContext.
                // For chooser rows, push the row's pre-built context onto the menu at
                // opening time — same mechanism the header path uses, just scoped to the
                // chooser ListBoxes.
                listBox.ContextMenuOpening += OnSectionContextMenuOpening;
            }

            // Move Up/Down buttons fire on whichever section row is currently selected.
            // We mirror the selection into SelectedColumn so a single CommandParameter
            // binding works regardless of which listbox owns the focus.
            if (GetTemplateChild("PART_MoveUpButton") is Button moveUpButton)
            {
                moveUpButton.Command = MoveUpCommand;
                moveUpButton.SetBinding(Button.CommandParameterProperty, new System.Windows.Data.Binding(nameof(SelectedColumn))
                {
                    Source = this,
                    Mode = System.Windows.Data.BindingMode.OneWay
                });
            }

            if (GetTemplateChild("PART_MoveDownButton") is Button moveDownButton)
            {
                moveDownButton.Command = MoveDownCommand;
                moveDownButton.SetBinding(Button.CommandParameterProperty, new System.Windows.Data.Binding(nameof(SelectedColumn))
                {
                    Source = this,
                    Mode = System.Windows.Data.BindingMode.OneWay
                });
            }
        }

        private WWListBox _leftSectionListBox;
        private WWListBox _unpinnedSectionListBox;
        private WWListBox _rightSectionListBox;

        private IEnumerable<WWListBox> EnumerateSectionListBoxes()
        {
            if (_leftSectionListBox != null) yield return _leftSectionListBox;
            if (_unpinnedSectionListBox != null) yield return _unpinnedSectionListBox;
            if (_rightSectionListBox != null) yield return _rightSectionListBox;
        }

        private void DetachSectionListBoxes()
        {
            foreach (var listBox in EnumerateSectionListBoxes())
            {
                listBox.ItemReordered -= OnSectionListItemReordered;
                listBox.SelectionChanged -= OnSectionListBoxSelectionChanged;
                listBox.ContextMenuOpening -= OnSectionContextMenuOpening;
            }
            _leftSectionListBox = null;
            _unpinnedSectionListBox = null;
            _rightSectionListBox = null;
        }

        /// <summary>
        /// Wires the shared column-header context menu to the chooser row that opened it.
        /// The Style attaches the same <see cref="ContextMenu"/> resource the column header
        /// uses (the resource is declared <c>x:Shared="False"</c>, so every ListBoxItem gets
        /// its own instance), but the menu's bindings target a
        /// <see cref="ContextMenuContext"/> that's specific to the column being acted on.
        /// This handler walks from the event source up to the originating
        /// <see cref="ListBoxItem"/>, reads the row's pre-built
        /// <see cref="ColumnVisibilityInfo.ContextMenuContext"/>, and pushes it onto the
        /// ContextMenu's <see cref="System.Windows.FrameworkElement.DataContext"/> before
        /// the menu opens. Mirrors the runtime DataContext assignment that
        /// <see cref="ContextMenuExtensions.OnContextMenuOpening"/> performs for header
        /// right-clicks, so commands behave identically across the two surfaces.
        /// </summary>
        private void OnSectionContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is not WWListBox listBox) return;

            // Walk up to the originating ListBoxItem so the chooser-row identity is preserved
            // even when the click lands on a child element (checkbox, text, glyph).
            var origin = e.OriginalSource as DependencyObject;
            ListBoxItem item = null;
            var cursor = origin;
            while (cursor != null)
            {
                if (cursor is ListBoxItem candidate)
                {
                    item = candidate;
                    break;
                }
                cursor = System.Windows.Media.VisualTreeHelper.GetParent(cursor)
                         ?? LogicalTreeHelper.GetParent(cursor);
            }

            if (item?.DataContext is not ColumnVisibilityInfo columnInfo || columnInfo.ContextMenuContext == null)
            {
                // No identifiable row (e.g., right-click on listbox background) — suppress the
                // menu rather than show a half-bound copy whose commands would no-op.
                e.Handled = true;
                return;
            }

            Core.CommandManager.InvalidateRequerySuggested();

            var menu = item.ContextMenu ?? listBox.ContextMenu;
            if (menu == null)
            {
                e.Handled = true;
                return;
            }

            menu.DataContext = columnInfo.ContextMenuContext;
            menu.PlacementTarget = item;
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

            // No local WindowStyle assignment — the chrome style sets WindowStyle=None, and a
            // local value here would override the style setter and break the custom chrome.
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
                Owner = ownerWindow,
            };

            // Track the initial owner window position
            if (_ownerWindow != null)
            {
                _lastOwnerPosition = new Point(_ownerWindow.Left, _ownerWindow.Top);
            }

            // Apply custom window style if provided; otherwise the shared PrimitivesWindow
            // chrome. Either way the caption-button SystemCommands get instance bindings.
            if (WindowStyle != null)
            {
                _parentWindow.Style = WindowStyle;
                WindowHostHelper.WireSystemCommands(_parentWindow);
            }
            else
            {
                WindowHostHelper.ApplyDefaultChrome(_parentWindow, this);
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
        /// Handles changes to the source data grid. Rebuilds the column lists and
        /// re-subscribes the <see cref="DataGrid.CanUserReorderColumns"/> listener
        /// to the new grid (detaching from the old) so per-section drag-enable
        /// flags stay accurate when the consumer toggles reordering at runtime.
        /// </summary>
        private static void OnSourceDataGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnChooser chooser) return;

            var canReorderDpd = DependencyPropertyDescriptor.FromProperty(
                DataGrid.CanUserReorderColumnsProperty, typeof(DataGrid));

            if (e.OldValue is DataGrid oldGrid)
                canReorderDpd?.RemoveValueChanged(oldGrid, chooser._onCanUserReorderColumnsChanged);

            if (e.NewValue is DataGrid newGrid)
                canReorderDpd?.AddValueChanged(newGrid, chooser._onCanUserReorderColumnsChanged);

            chooser.RefreshColumns();
            chooser.UpdateSectionFlags();
        }

        private EventHandler _onCanUserReorderColumnsChanged;

        /// <summary>
        /// Refreshes all four chooser collections from the source data grid.
        /// Iterates in <see cref="DataGridColumn.DisplayIndex"/> order, builds one
        /// <see cref="ColumnVisibilityInfo"/> per visible descriptor, and partitions
        /// the results into <see cref="LeftFixedColumns"/> / <see cref="UnpinnedColumns"/>
        /// / <see cref="RightFixedColumns"/> based on each descriptor's
        /// <see cref="GridColumn.Fixed"/> value. <see cref="Columns"/> is rebuilt as
        /// the concatenation Left → Unpinned → Right so callers iterating it see the
        /// same order the user sees in the chooser.
        /// </summary>
        internal void RefreshColumns()
        {
            Columns.Clear();
            LeftFixedColumns.Clear();
            UnpinnedColumns.Clear();
            RightFixedColumns.Clear();

            if (SourceDataGrid?.Columns == null) return;

            foreach (DataGridColumn column in SourceDataGrid.Columns.OrderBy(c => c.DisplayIndex))
            {
                var descriptor = SourceDataGrid.FindGridColumnDescriptor(column);
                if (descriptor == null)
                    continue;

                if (!descriptor.ShowInColumnChooser)
                    continue;

                string displayName = !string.IsNullOrEmpty(descriptor.ColumnDisplayName)
                    ? descriptor.ColumnDisplayName
                    : descriptor.HeaderCaption;

                // Look up the column's filter host so the row can mirror the column header's
                // filter-active glyph and so the shared header context menu (which binds to a
                // ContextMenuContext.ColumnSearchBox) operates against the same instance the
                // header context menu would.
                var filterHost = SourceDataGrid.DataColumns?
                    .FirstOrDefault(c => c.CurrentColumn == column);

                var columnInfo = new ColumnVisibilityInfo
                {
                    Column = column,
                    GridColumnDescriptor = descriptor,
                    DisplayName = displayName ?? "Unknown Column",
                    IsVisible = column.Visibility == Visibility.Visible,
                    FixedPosition = descriptor.Fixed,
                    ColumnFilterHost = filterHost,
                    ContextMenuContext = new ContextMenuContext
                    {
                        ContextType = ContextMenuType.ColumnHeader,
                        Grid = SourceDataGrid,
                        Column = column,
                        ColumnSearchBox = filterHost,
                    }
                };

                columnInfo.PropertyChanged += OnColumnInfoPropertyChanged;

                // Master list first so PropertyChanged subscription is established
                // before the section listbox observes the item.
                Columns.Add(columnInfo);
                GetSectionFor(columnInfo.FixedPosition).Add(columnInfo);
            }

            // Selection survives only if the previously selected item is still in
            // the rebuilt master list — clear otherwise so the Move buttons disable.
            if (SelectedColumn != null && !Columns.Contains(SelectedColumn))
                SelectedColumn = null;
        }

        /// <summary>
        /// Returns the section <see cref="ObservableCollection{T}"/> that hosts a column with
        /// the given <see cref="FixedColumnPosition"/>.
        /// </summary>
        private ObservableCollection<ColumnVisibilityInfo> GetSectionFor(FixedColumnPosition position)
            => position switch
            {
                FixedColumnPosition.Left => LeftFixedColumns,
                FixedColumnPosition.Right => RightFixedColumns,
                _ => UnpinnedColumns,
            };

        /// <summary>
        /// Returns the section <see cref="ObservableCollection{T}"/> that currently
        /// hosts <paramref name="columnInfo"/>, falling back to the column's
        /// <see cref="ColumnVisibilityInfo.FixedPosition"/> when the column isn't
        /// found in any list yet.
        /// </summary>
        private ObservableCollection<ColumnVisibilityInfo> GetHostSection(ColumnVisibilityInfo columnInfo)
        {
            if (columnInfo == null) return UnpinnedColumns;
            if (LeftFixedColumns.Contains(columnInfo)) return LeftFixedColumns;
            if (UnpinnedColumns.Contains(columnInfo)) return UnpinnedColumns;
            if (RightFixedColumns.Contains(columnInfo)) return RightFixedColumns;
            return GetSectionFor(columnInfo.FixedPosition);
        }

        /// <summary>
        /// Handles property changes on column info objects
        /// </summary>
        private void OnColumnInfoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ColumnVisibilityInfo.IsVisible) && sender is ColumnVisibilityInfo columnInfo)
            {
                if (columnInfo.Column != null)
                    columnInfo.Column.Visibility = columnInfo.IsVisible ? Visibility.Visible : Visibility.Collapsed;

                if (columnInfo.GridColumnDescriptor != null)
                    columnInfo.GridColumnDescriptor.Visible = columnInfo.IsVisible;

                // Schedule a deferred scroll metrics refresh.
                // During bulk operations (Select All), this coalesces into a single update.
                ScheduleScrollMetricsRefresh();
            }
        }

        /// <summary>
        /// Schedules a deferred refresh of the DataGrid's scroll metrics after column visibility changes.
        /// Coalesces rapid calls (e.g., from Select All) into a single update by aborting any pending refresh.
        /// </summary>
        private void ScheduleScrollMetricsRefresh()
        {
            if (SourceDataGrid == null)
                return;

            _pendingScrollRefresh?.Abort();

            _pendingScrollRefresh = SourceDataGrid.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var scrollViewer = VisualTreeHelperMethods.FindVisualDescendant<ScrollViewer>(SourceDataGrid);
                    if (scrollViewer != null)
                    {
                        scrollViewer.InvalidateScrollInfo();
                    }
                    SourceDataGrid.InvalidateMeasure();
                    SourceDataGrid.UpdateLayout();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error refreshing DataGrid scroll metrics: {ex.Message}");
                }
            }), DispatcherPriority.Input);
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
        /// Cross-section single-selection sync. When one section listbox gains a
        /// selection, clear the other two so only one row is highlighted across
        /// the whole chooser. Also mirrors the selected item into
        /// <see cref="SelectedColumn"/>, which the Move Up / Move Down buttons
        /// bind their CommandParameter to.
        /// </summary>
        private void OnSectionListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingSelection) return;
            if (sender is not WWListBox source) return;

            _isSyncingSelection = true;
            try
            {
                var newlySelected = source.SelectedItem as ColumnVisibilityInfo;

                // Clear selection in the sibling listboxes so only one chooser row
                // is highlighted at a time. Skipping the active one prevents the
                // reentrant SelectionChanged loop the guard above also catches.
                foreach (var listBox in EnumerateSectionListBoxes())
                {
                    if (listBox != source)
                        listBox.SelectedItem = null;
                }

                // Mirror IsSelected onto every ColumnVisibilityInfo so item-level
                // styling stays consistent across sections.
                foreach (var column in Columns)
                    column.IsSelected = (column == newlySelected);

                SelectedColumn = newlySelected;
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private bool _isSyncingSelection;

        /// <summary>
        /// Handles drag-drop reordering inside one of the three section listboxes.
        /// The section structure enforces the group constraint by construction —
        /// drag-drop never crosses a section boundary because each section is its
        /// own <see cref="WWListBox"/> — so this handler only needs to
        /// apply the move within the host section.
        /// </summary>
        private void OnSectionListItemReordered(object sender, ItemReorderedEventArgs e)
        {
            if (e.Item is not ColumnVisibilityInfo columnInfo || SourceDataGrid == null)
                return;
            if (columnInfo.Column == null) return;

            var section = GetHostSection(columnInfo);
            int oldIndex = e.OldIndex;
            int newIndex = e.NewIndex;

            if (oldIndex < 0 || newIndex < 0) return;
            if (oldIndex >= section.Count || newIndex >= section.Count) return;
            if (oldIndex == newIndex) return;

            ApplyChooserMove(columnInfo, section, oldIndex, newIndex);
        }

        /// <summary>
        /// Applies a same-section reorder to the host <paramref name="section"/> collection,
        /// rebuilds the master <see cref="Columns"/> order from the three sections, then
        /// re-stamps <see cref="DataGridColumn.DisplayIndex"/> on every grid column from
        /// that order and asks the grid to reapply its fixed-column layout. The
        /// <see cref="SearchDataGrid.ApplyFixedColumnLayout"/> call keeps
        /// <see cref="DataGrid.FrozenColumnCount"/> in sync — important because a reorder
        /// inside the Left section can change which descriptors are first, even though the
        /// count of left-pinned columns hasn't changed.
        /// </summary>
        private void ApplyChooserMove(
            ColumnVisibilityInfo columnInfo,
            ObservableCollection<ColumnVisibilityInfo> section,
            int oldIndex,
            int newIndex)
        {
            if (section == null) return;
            if (oldIndex == newIndex) return;
            if (oldIndex < 0 || oldIndex >= section.Count) return;
            if (newIndex < 0 || newIndex >= section.Count) return;

            _isApplyingInternalMove = true;
            try
            {
                section.Move(oldIndex, newIndex);

                // Rebuild the master flat list from the three section orders so
                // anything iterating Columns (e.g., select-all calculations) sees
                // the same order the user is looking at.
                RebuildMasterColumnsFromSections();

                // Reassign DisplayIndex from the new master order. Iterating by master
                // position keeps unpinned columns' relative order intact and gives the
                // grid a contiguous index space that ApplyFixedColumnLayout will treat
                // as the desired Left/None/Right sequence.
                for (int i = 0; i < Columns.Count; i++)
                {
                    var col = Columns[i].Column;
                    if (col != null && col.DisplayIndex != i)
                        col.DisplayIndex = i;
                }

                SourceDataGrid.ApplyFixedColumnLayout();
                SourceDataGrid.UpdateLayout();

                MoveUpCommand.RaiseCanExecuteChanged();
                MoveDownCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChooserMove] ERROR: {ex.Message}");
                Debug.WriteLine($"[ChooserMove] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                _isApplyingInternalMove = false;
            }
        }

        /// <summary>
        /// Rebuilds <see cref="Columns"/> by concatenating the three section collections
        /// in Left → Unpinned → Right order. Items are moved (not re-added) to preserve
        /// their <see cref="ColumnVisibilityInfo.PropertyChanged"/> subscriptions on the
        /// master-list path.
        /// </summary>
        private void RebuildMasterColumnsFromSections()
        {
            var desired = new List<ColumnVisibilityInfo>(
                LeftFixedColumns.Count + UnpinnedColumns.Count + RightFixedColumns.Count);
            desired.AddRange(LeftFixedColumns);
            desired.AddRange(UnpinnedColumns);
            desired.AddRange(RightFixedColumns);

            // Use Move so existing PropertyChanged subscriptions stay bound. We only
            // get here when ordering changed within one section, so the set is identical;
            // only relative ordering differs.
            for (int target = 0; target < desired.Count; target++)
            {
                var item = desired[target];
                int current = Columns.IndexOf(item);
                if (current < 0)
                {
                    // Shouldn't happen — section lists are partitions of Columns — but
                    // recover by inserting rather than throwing.
                    item.PropertyChanged += OnColumnVisibilityChanged;
                    Columns.Insert(target, item);
                }
                else if (current != target)
                {
                    Columns.Move(current, target);
                }
            }
        }

        /// <summary>
        /// True while <see cref="ApplyChooserMove"/> is updating the grid. Guards
        /// <see cref="OnGridFixedColumnLayoutChanged"/> from rebuilding the chooser list
        /// in response to our own layout call, which would discard the in-flight selection.
        /// </summary>
        private bool _isApplyingInternalMove;

        /// <summary>
        /// Called by <see cref="SearchDataGrid.ApplyFixedColumnLayout"/> when a column's
        /// <see cref="GridColumn.Fixed"/> value changes (typically via the column-header
        /// "Pin Left / Pin Right / Unpin" context menu). Rebuilds the chooser list so
        /// row order, pin glyphs, and move-arrow enablement reflect the new pinning state.
        /// </summary>
        internal void OnGridFixedColumnLayoutChanged()
        {
            if (_isApplyingInternalMove) return;
            if (SourceDataGrid == null) return;

            RefreshColumns();
            MoveUpCommand.RaiseCanExecuteChanged();
            MoveDownCommand.RaiseCanExecuteChanged();
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
        /// Moves the specified column up or down within its host section.
        /// The section structure enforces the group constraint, so this is a
        /// straightforward bounds-check inside the section.
        /// </summary>
        private void MoveColumn(ColumnVisibilityInfo columnInfo, int direction)
        {
            if (columnInfo?.Column == null || SourceDataGrid == null)
                return;

            var section = GetHostSection(columnInfo);
            int currentIndex = section.IndexOf(columnInfo);
            if (currentIndex < 0)
                return;

            int newIndex = currentIndex + direction;
            if (newIndex < 0 || newIndex >= section.Count)
                return;

            ApplyChooserMove(columnInfo, section, currentIndex, newIndex);
        }

        /// <summary>
        /// Determines whether the specified column can be moved one step in the given
        /// direction inside its host section. Returns <c>false</c> at the top of the
        /// section for direction = -1 and at the bottom for direction = +1.
        /// </summary>
        private bool CanMoveColumn(ColumnVisibilityInfo columnInfo, int direction)
        {
            if (SourceDataGrid == null) return false;
            if (!SourceDataGrid.CanUserReorderColumns) return false;
            if (columnInfo == null) return false;

            var section = GetHostSection(columnInfo);
            int currentIndex = section.IndexOf(columnInfo);
            if (currentIndex < 0) return false;

            int newIndex = currentIndex + direction;
            return newIndex >= 0 && newIndex < section.Count;
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
        private FixedColumnPosition _fixedPosition;

        public DataGridColumn Column { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="GridColumn"/> descriptor that generated
        /// <see cref="Column"/>, or null for legacy attached-property columns.
        /// </summary>
        public GridColumn GridColumnDescriptor { get; set; }

        /// <summary>
        /// The <see cref="IColumnFilterHost"/> for this column (its
        /// <see cref="ColumnFilterControl"/> in the auto-filter row), or <c>null</c> if the
        /// column has no filter editor. Exposed for binding so the chooser row can mirror
        /// the column header's filter-active glyph reactively — the host's
        /// <see cref="IColumnFilterHost.HasActiveFilter"/> is a <see cref="DependencyProperty"/>
        /// on <see cref="ColumnFilterControl"/>, so the binding updates as filters come and go.
        /// </summary>
        public IColumnFilterHost ColumnFilterHost { get; set; }

        /// <summary>
        /// The <see cref="ContextMenuContext"/> that the shared column-header context menu
        /// binds against when invoked from this chooser row. Constructed once per row in
        /// <see cref="ColumnChooser.RefreshColumns"/> and remains valid for the row's lifetime
        /// — the column reference and Grid are stable; the filter host reference is captured
        /// from <see cref="SearchDataGrid.DataColumns"/> at the same time. Commands sourced
        /// from the shared menu (sort / hide / pin / clear filter / …) read Grid, Column, and
        /// ColumnSearchBox off this object exactly the same way they would from a header click.
        /// </summary>
        public ContextMenuContext ContextMenuContext { get; set; }

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

        /// <summary>
        /// Mirrors <see cref="GridColumn.Fixed"/> so the chooser can apply per-row
        /// styling (pin glyph, dimmed move arrows) and constrain reorder operations
        /// to within a single pinning group.
        /// </summary>
        public FixedColumnPosition FixedPosition
        {
            get => _fixedPosition;
            set
            {
                if (SetProperty(value, ref _fixedPosition, nameof(FixedPosition)))
                    OnPropertyChanged(nameof(IsFixed));
            }
        }

        /// <summary>Convenience for binding — true when <see cref="FixedPosition"/> is not <c>None</c>.</summary>
        public bool IsFixed => _fixedPosition != FixedColumnPosition.None;
    }

    #endregion
}
