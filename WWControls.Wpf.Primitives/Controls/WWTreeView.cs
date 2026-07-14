using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf.Primitives
{
    /// <summary>
    /// A <see cref="TreeView"/> that adds two-way single-selection binding (<see cref="SelectedObject"/>),
    /// optional drag-and-drop reordering routed through <see cref="OnDropCommand"/>, expand-on-load,
    /// and expand-all / collapse-all commands. Its containers are <see cref="WWTreeViewItem"/>s, which
    /// draw the connector lines and host the per-item expand/collapse affordances.
    /// </summary>
    public class WWTreeView : TreeView, IDisposable
    {
        #region Private Fields

        private bool _disposed = false;

        #endregion

        #region Constructors and Finalizers

        static WWTreeView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWTreeView), new FrameworkPropertyMetadata(typeof(WWTreeView)));
        }

        public WWTreeView()
        {
            this.Loaded += OnLoaded;
        }

        ~WWTreeView()
        {
            Dispose(false);
        }

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register(
                nameof(SelectedObject),
                typeof(object),
                typeof(WWTreeView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedObjectPropertyChanged));

        public object SelectedObject
        {
            get => GetValue(SelectedObjectProperty);
            set => SetValue(SelectedObjectProperty, value);
        }

        public static readonly DependencyProperty ExpandOnLoadProperty =
            DependencyProperty.Register(
                nameof(ExpandOnLoad),
                typeof(bool),
                typeof(WWTreeView),
                new PropertyMetadata(false));

        public bool ExpandOnLoad
        {
            get => (bool)GetValue(ExpandOnLoadProperty);
            set => SetValue(ExpandOnLoadProperty, value);
        }

        public static readonly DependencyProperty EnableDoubleClickExpandProperty =
            DependencyProperty.Register(
                nameof(EnableDoubleClickExpand),
                typeof(bool),
                typeof(WWTreeView),
                new PropertyMetadata(true));

        public bool EnableDoubleClickExpand
        {
            get => (bool)GetValue(EnableDoubleClickExpandProperty);
            set => SetValue(EnableDoubleClickExpandProperty, value);
        }

        public static readonly DependencyProperty AllowDragDropProperty =
        DependencyProperty.Register(
            nameof(AllowDragDrop),
            typeof(bool),
            typeof(WWTreeView),
            new PropertyMetadata(false));

        public bool AllowDragDrop
        {
            get => (bool)GetValue(AllowDragDropProperty);
            set => SetValue(AllowDragDropProperty, value);
        }

        public static readonly DependencyProperty OnDropCommandProperty =
            DependencyProperty.Register(
                nameof(OnDropCommand),
                typeof(ICommand),
                typeof(WWTreeView),
                new PropertyMetadata(null));

        public ICommand OnDropCommand
        {
            get => (ICommand)GetValue(OnDropCommandProperty);
            set => SetValue(OnDropCommandProperty, value);
        }

        public static readonly DependencyProperty ShowExpandCollapseButtonsProperty =
            DependencyProperty.Register(
                nameof(ShowExpandCollapseButtons),
                typeof(bool),
                typeof(WWTreeView),
                new PropertyMetadata(false));

        /// <summary>
        /// When true, shows expand all / collapse all buttons on tree view items on hover.
        /// </summary>
        public bool ShowExpandCollapseButtons
        {
            get => (bool)GetValue(ShowExpandCollapseButtonsProperty);
            set => SetValue(ShowExpandCollapseButtonsProperty, value);
        }

        public static readonly DependencyProperty ExpandCollapseButtonModeProperty =
            DependencyProperty.Register(
                nameof(ExpandCollapseButtonMode),
                typeof(ExpandCollapseButtonVisibility),
                typeof(WWTreeView),
                new PropertyMetadata(ExpandCollapseButtonVisibility.HasGrandchildren));

        /// <summary>
        /// Gets or sets when expand/collapse all buttons should be shown on tree view items.
        /// </summary>
        public ExpandCollapseButtonVisibility ExpandCollapseButtonMode
        {
            get => (ExpandCollapseButtonVisibility)GetValue(ExpandCollapseButtonModeProperty);
            set => SetValue(ExpandCollapseButtonModeProperty, value);
        }

        public static readonly DependencyProperty ExpandAllCommandProperty =
            DependencyProperty.Register(
                nameof(ExpandAllCommand),
                typeof(ICommand),
                typeof(WWTreeView),
                new PropertyMetadata(null));

        /// <summary>
        /// Command that expands all nodes in the tree. Bind this to a button's Command property.
        /// </summary>
        public ICommand ExpandAllCommand
        {
            get => (ICommand)GetValue(ExpandAllCommandProperty);
            set => SetValue(ExpandAllCommandProperty, value);
        }

        public static readonly DependencyProperty CollapseAllCommandProperty =
            DependencyProperty.Register(
                nameof(CollapseAllCommand),
                typeof(ICommand),
                typeof(WWTreeView),
                new PropertyMetadata(null));

        /// <summary>
        /// Command that collapses all nodes in the tree. Bind this to a button's Command property.
        /// </summary>
        public ICommand CollapseAllCommand
        {
            get => (ICommand)GetValue(CollapseAllCommandProperty);
            set => SetValue(CollapseAllCommandProperty, value);
        }

        /// <summary>
        /// Expands or collapses all root-level items and their descendants.
        /// </summary>
        public void SetAllExpanded(bool expanded)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var container = ItemContainerGenerator.ContainerFromIndex(i) as WWTreeViewItem;
                container?.SetAllExpanded(expanded);
            }
        }


        #endregion

        #region Event Handlers

        private static void OnSelectedObjectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WWTreeView treeView && e.NewValue == null && treeView.SelectedItem != null)
            {
                // WPF TreeView doesn't natively support deselection. When the bound
                // SelectedObject is set to null, find the currently selected container
                // and explicitly deselect it so the tree doesn't show a stale highlight.
                if (treeView.ItemContainerGenerator.ContainerFromItem(treeView.SelectedItem) is TreeViewItem container)
                {
                    container.IsSelected = false;
                }
            }
        }

        private static void OnTreeViewSelectedObjectChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is WWTreeView treeView)
            {
                treeView.SelectedObject = e.NewValue;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Guard against duplicate subscriptions from Loaded/Unloaded cycles
            this.SelectedItemChanged -= OnTreeViewSelectedObjectChanged;
            this.SelectedItemChanged += OnTreeViewSelectedObjectChanged;

            if (ExpandAllCommand == null)
                ExpandAllCommand = new RelayCommand(_ => SetAllExpanded(true));

            if (CollapseAllCommand == null)
                CollapseAllCommand = new RelayCommand(_ => SetAllExpanded(false));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            this.SelectedItemChanged -= OnTreeViewSelectedObjectChanged;
        }

        #endregion

        #region Overrides

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new WWTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is WWTreeViewItem;
        }

        /// <summary>
        /// Called by WPF for EVERY container being removed — including virtualized (off-screen) ones.
        /// This is the reliable cleanup hook that ContainerFromIndex misses.
        /// </summary>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            if (element is WWTreeViewItem tvi)
                tvi.Dispose();

            base.ClearContainerForItemOverride(element, item);
        }

        #endregion

        #region IDisposable Implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    this.Loaded -= OnLoaded;
                    this.SelectedItemChanged -= OnTreeViewSelectedObjectChanged;

                    // Disable container recycling to ensure ClearContainerForItemOverride
                    // is called for all containers (recycled containers are otherwise pooled).
                    VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Standard);

                    // Sever ItemsSource completely — ClearValue handles both bound and
                    // programmatic cases and automatically empties the Items collection.
                    ClearValue(ItemsSourceProperty);
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
