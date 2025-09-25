using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;

namespace WWSearchDataGrid.Modern.WPF.Controls.Primitives
{
    public class HighlightTextBlock : TextBlock
    {

        #region Dependency Properties

        public static readonly DependencyProperty HighlightTextProperty =
           DependencyProperty.Register(
               "HighlightText",
               typeof(string),
               typeof(HighlightTextBlock),
               new PropertyMetadata(string.Empty, OnHighlightTextChanged));

        public static readonly DependencyProperty HighlightTextBlockTextProperty =
            DependencyProperty.Register(
                "HighlightTextBlockText",
                typeof(string),
                typeof(HighlightTextBlock),
                new PropertyMetadata(string.Empty, OnTextChanged));

        #endregion Dependency Properties

        #region Properties

        public string HighlightText
        {
            get => (string)GetValue(HighlightTextProperty);
            set => SetValue(HighlightTextProperty, value);
        }
        public string HighlightTextBlockText
        {
            get => (string)GetValue(HighlightTextBlockTextProperty);
            set => SetValue(HighlightTextBlockTextProperty, value);
        }

        #endregion Properties

        #region Event Handlers

        private static void OnHighlightTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HighlightTextBlock textBlock)
            {
                textBlock.Highlight();
            }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HighlightTextBlock textBlock)
            {
                textBlock.Highlight();
            }
        }

        #endregion Event Handlers

        #region Methods

        private void Highlight()
        {
            Inlines.Clear();
            if (string.IsNullOrEmpty(HighlightTextBlockText) || string.IsNullOrEmpty(HighlightText))
            {
                Inlines.Add(new Run(HighlightTextBlockText));
                return;
            }

            var textLower = HighlightTextBlockText.ToLower();
            var highlightLower = HighlightText.ToLower();

            // Use Contains logic
            int index = textLower.IndexOf(highlightLower);

            if (index < 0)
            {
                Inlines.Add(new Run(HighlightTextBlockText));
                return;
            }

            int highlightLength = HighlightText.Length;

            // Add text before the match
            if (index > 0)
            {
                Inlines.Add(new Run(HighlightTextBlockText.Substring(0, index)));
            }

            // Add the matching text with highlight
            var match = new Run(HighlightTextBlockText.Substring(index, highlightLength)) { FontWeight = FontWeights.UltraBlack };
            Inlines.Add(match);

            // Add text after the match
            if (index + highlightLength < HighlightTextBlockText.Length)
            {
                Inlines.Add(new Run(HighlightTextBlockText.Substring(index + highlightLength)));
            }
        }


        #endregion Methods
    }
}
