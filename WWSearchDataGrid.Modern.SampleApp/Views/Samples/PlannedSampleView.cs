using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples
{
    /// <summary>
    /// A fully navigable placeholder for a sample that isn't built yet. Rather than hiding such
    /// samples, the catalog points their factory here so the launcher shows the full target layout
    /// as a live checklist: a badge, a short summary of what the sample will demonstrate, and the
    /// concrete work items needed to build it. Built in code (no XAML) so one type serves every
    /// stub entry.
    /// <para>
    /// Two flavours via the static factories: <see cref="Planned"/> (the underlying library feature
    /// doesn't exist yet) and <see cref="SamplePending"/> (the feature ships today, only the sample
    /// view is outstanding).
    /// </para>
    /// </summary>
    public sealed class PlannedSampleView : UserControl
    {
        private PlannedSampleView(string badge, Color badgeBg, Color badgeBorder, Color badgeFg,
            string title, string summary, string[] requirements)
        {
            Content = BuildContent(badge, badgeBg, badgeBorder, badgeFg, title, summary,
                requirements ?? Array.Empty<string>());
        }

        /// <summary>Feature not implemented in the library yet.</summary>
        public static PlannedSampleView Planned(string title, string summary, params string[] requirements) =>
            new("🚧  Planned — not yet implemented",
                Color.FromRgb(0xFF, 0xF3, 0xCD), Color.FromRgb(0xFF, 0xE0, 0x69), Color.FromRgb(0x85, 0x64, 0x04),
                title, summary, requirements);

        /// <summary>Feature already ships; only the dedicated sample view is outstanding.</summary>
        public static PlannedSampleView SamplePending(string title, string summary, params string[] requirements) =>
            new("🔧  Sample pending — feature available today",
                Color.FromRgb(0xD1, 0xEC, 0xF1), Color.FromRgb(0x9E, 0xD4, 0xDF), Color.FromRgb(0x0C, 0x54, 0x60),
                title, summary, requirements);

        private static UIElement BuildContent(string badge, Color badgeBg, Color badgeBorder, Color badgeFg,
            string title, string summary, string[] requirements)
        {
            var card = new StackPanel
            {
                MaxWidth = 560,
                Margin = new Thickness(32),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            card.Children.Add(new Border
            {
                Background = new SolidColorBrush(badgeBg),
                BorderBrush = new SolidColorBrush(badgeBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 4, 10, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 12),
                Child = new TextBlock
                {
                    Text = badge,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(badgeFg),
                },
            });

            card.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22)),
                Margin = new Thickness(0, 0, 0, 6),
                TextWrapping = TextWrapping.Wrap,
            });

            card.Children.Add(new TextBlock
            {
                Text = summary,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16),
            });

            if (requirements.Length > 0)
            {
                card.Children.Add(new TextBlock
                {
                    Text = "To implement:",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                    Margin = new Thickness(0, 0, 0, 6),
                });

                foreach (var item in requirements)
                {
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
                    row.Children.Add(new TextBlock
                    {
                        Text = "•",
                        FontSize = 13,
                        Margin = new Thickness(0, 0, 8, 0),
                        Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    });
                    row.Children.Add(new TextBlock
                    {
                        Text = item,
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 500,
                    });
                    card.Children.Add(row);
                }
            }

            return new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = card,
            };
        }
    }
}
