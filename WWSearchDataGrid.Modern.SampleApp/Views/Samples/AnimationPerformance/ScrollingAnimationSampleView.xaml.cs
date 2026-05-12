using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using WWSearchDataGrid.Modern.WPF;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.AnimationPerformance
{
    public partial class ScrollingAnimationSampleView : UserControl
    {
        public ScrollingAnimationSampleView() => InitializeComponent();

        // Custom scroll animation: exponential ease-out via Storyboard. Wired only when ScrollAnimationMode=Custom.
        private void OnCustomScrollAnimation(object? sender, CustomScrollAnimationEventArgs e)
        {
            var animation = new DoubleAnimation
            {
                From = e.OldOffset,
                To = e.NewOffset,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 6 }
            };
            Storyboard.SetTargetProperty(animation,
                new PropertyPath("(0)", SmoothScrollBehavior.AnimatedVerticalOffsetProperty));

            var sb = new Storyboard();
            sb.Children.Add(animation);
            e.Storyboard = sb;
        }
    }
}
