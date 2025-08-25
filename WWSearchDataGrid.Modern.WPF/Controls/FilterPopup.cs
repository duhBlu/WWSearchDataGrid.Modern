using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Custom popup control for hosting filter editors with smart positioning and auto-dismiss behavior
    /// </summary>
    public class FilterPopup : Control, INotifyPropertyChanged
    {
        #region Fields

        private Popup _popup;
        private FrameworkElement _placementTarget;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(FilterPopup),
                new PropertyMetadata(false, OnIsOpenChanged));

        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.Register(nameof(Placement), typeof(PlacementMode), typeof(FilterPopup),
                new PropertyMetadata(PlacementMode.Bottom));

        public static readonly DependencyProperty MaxWidthProperty =
            DependencyProperty.Register(nameof(MaxWidth), typeof(double), typeof(FilterPopup),
                new PropertyMetadata(600.0));

        public static readonly DependencyProperty MaxHeightProperty =
            DependencyProperty.Register(nameof(MaxHeight), typeof(double), typeof(FilterPopup),
                new PropertyMetadata(500.0));

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content), typeof(object), typeof(FilterPopup),
                new PropertyMetadata(null));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether the popup is open
        /// </summary>
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        /// <summary>
        /// Gets or sets the placement mode for the popup
        /// </summary>
        public PlacementMode Placement
        {
            get => (PlacementMode)GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum width of the popup
        /// </summary>
        public new double MaxWidth
        {
            get => (double)GetValue(MaxWidthProperty);
            set => SetValue(MaxWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum height of the popup
        /// </summary>
        public new double MaxHeight
        {
            get => (double)GetValue(MaxHeightProperty);
            set => SetValue(MaxHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the content of the popup
        /// </summary>
        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the placement target for the popup
        /// </summary>
        public FrameworkElement PlacementTarget
        {
            get => _placementTarget;
            set
            {
                if (_placementTarget != value)
                {
                    _placementTarget = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the popup is opened
        /// </summary>
        public event EventHandler Opened;

        /// <summary>
        /// Occurs when the popup is closed
        /// </summary>
        public event EventHandler Closed;

        #endregion

        #region Constructors

        public FilterPopup()
        {
            Unloaded += OnUnloaded;
            
            // Create popup immediately so it's always available
            CreatePopupDirectly();
        }

        #endregion


        #region Private Methods

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FilterPopup filterPopup)
            {
                filterPopup.OnIsOpenChanged((bool)e.NewValue);
            }
        }

        private void OnIsOpenChanged(bool isOpen)
        {
            if (_popup == null) return;

            if (isOpen)
            {
                ShowPopup();
            }
            else
            {
                HidePopup();
            }
        }

        private void HidePopup()
        {
            if (_popup == null) return;

            try
            {
                _popup.IsOpen = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding popup: {ex.Message}");
            }
        }

        private void CreatePopupDirectly()
        {
            _popup = new System.Windows.Controls.Primitives.Popup
            {
                AllowsTransparency = true,
                PopupAnimation = System.Windows.Controls.Primitives.PopupAnimation.Fade,
                StaysOpen = false,
                Placement = Placement
            };

            _popup.Opened += OnPopupOpened;
            _popup.Closed += OnPopupClosed;
            _popup.KeyDown += OnPopupKeyDown;

            // Bind properties
            var placementBinding = new System.Windows.Data.Binding(nameof(Placement))
            {
                Source = this,
                Mode = System.Windows.Data.BindingMode.OneWay
            };
            _popup.SetBinding(System.Windows.Controls.Primitives.Popup.PlacementProperty, placementBinding);

            // Don't bind IsOpen directly - manage it manually to avoid sync issues

            CreatePopupContent();
        }

        private void CreatePopupContent()
        {
            var border = new Border
            {
                Background = SystemColors.WindowBrush,
                BorderBrush = SystemColors.ActiveBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                MaxWidth = MaxWidth,
                MaxHeight = MaxHeight,
                Margin = new Thickness(10),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = System.Windows.Media.Colors.Black,
                    Opacity = 0.2,
                    BlurRadius = 8,
                    ShadowDepth = 2
                }
            };

            var contentPresenter = new ContentPresenter
            {
                Margin = new Thickness(8)
            };

            // Bind the content
            var contentBinding = new System.Windows.Data.Binding(nameof(Content))
            {
                Source = this,
                Mode = System.Windows.Data.BindingMode.OneWay
            };
            contentPresenter.SetBinding(ContentPresenter.ContentProperty, contentBinding);

            border.Child = contentPresenter;
            _popup.Child = border;
        }


        private void ShowPopup()
        {
            if (_popup == null) return;

            try
            {
                // Update placement target if needed
                if (PlacementTarget != null)
                {
                    _popup.PlacementTarget = PlacementTarget;
                }

                _popup.IsOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing popup: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_popup != null)
            {
                _popup.IsOpen = false;
                _popup.Opened -= OnPopupOpened;
                _popup.Closed -= OnPopupClosed;
                _popup.KeyDown -= OnPopupKeyDown;
            }
        }

        private void OnPopupOpened(object sender, EventArgs e)
        {
            Opened?.Invoke(this, EventArgs.Empty);
        }

        private void OnPopupClosed(object sender, EventArgs e)
        {
            // Ensure our IsOpen property stays in sync when popup closes
            if (IsOpen)
            {
                IsOpen = false;
            }
            
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void OnPopupKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                IsOpen = false;
                e.Handled = true;
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}