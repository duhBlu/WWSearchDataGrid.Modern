using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;

namespace WWSearchDataGrid.Modern.WPF
{


    /// <summary>
    /// A TextBlock that highlights the first occurrence of a search term within a source string.
    /// Bind <see cref="HighlightTextBlockText"/> to the full text and <see cref="HighlightText"/> to the term to match.
    /// The matching segment is emitted as a separate <see cref="Run"/> so you can style it independently.
    /// Use <see cref="HighlightRunStyle"/> to customize the appearance (e.g., FontWeight, FontStyle, Foreground, TextDecorations).
    ///
    /// Notes:
    /// - Matching is case-insensitive and highlights only the first match.
    /// - If either text is null/empty or no match is found, the control renders the full text without highlighting.
    /// </summary>
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

        /// <summary>
        /// A Style applied to the <see cref="Run"/> that represents the highlighted match.
        /// TargetType should be <see cref="Run"/>. If null, a bold (UltraBlack) weight is applied by default.
        /// </summary>
        public static readonly DependencyProperty HighlightRunStyleProperty =
            DependencyProperty.Register(
                "HighlightRunStyle",
                typeof(Style),
                typeof(HighlightTextBlock),
                new PropertyMetadata(null, OnHighlightRunStyleChanged));

        #endregion

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

        /// <summary>
        /// Style applied to the highlighted Run. Example:
        /// <code>
        /// &lt;local:HighlightTextBlock.HighlightRunStyle&gt;
        ///   &lt;Style TargetType="Run"&gt;
        ///     &lt;Setter Property="FontWeight" Value="SemiBold"/&gt;
        ///     &lt;Setter Property="FontStyle"  Value="Italic"/&gt;
        ///     &lt;Setter Property="Foreground" Value="{DynamicResource AccentBrush}"/&gt;
        ///   &lt;/Style&gt;
        /// &lt;/local:HighlightTextBlock.HighlightRunStyle&gt;
        /// </code>
        /// </summary>
        public Style HighlightRunStyle
        {
            get => (Style)GetValue(HighlightRunStyleProperty);
            set => SetValue(HighlightRunStyleProperty, value);
        }

        #endregion

        #region Property Changed Handlers

        private static void OnHighlightTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = d as HighlightTextBlock;
            if (textBlock != null) textBlock.Highlight();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = d as HighlightTextBlock;
            if (textBlock != null) textBlock.Highlight();
        }

        private static void OnHighlightRunStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = d as HighlightTextBlock;
            if (textBlock != null) textBlock.Highlight();
        }

        #endregion

        #region Highlight Logic

        private void Highlight()
        {
            Inlines.Clear();

            var text = HighlightTextBlockText ?? string.Empty;
            var term = HighlightText ?? string.Empty;

            if (text.Length == 0 || term.Length == 0)
            {
                Inlines.Add(new Run(text));
                return;
            }

            // Case-insensitive, first occurrence
            int index = text.IndexOf(term, StringComparison.CurrentCultureIgnoreCase);
            if (index < 0)
            {
                Inlines.Add(new Run(text));
                return;
            }

            int matchLen = term.Length;

            // Before match
            if (index > 0)
                Inlines.Add(new Run(text.Substring(0, index)));

            // Match (apply style or fallback weight)
            var matchRun = new Run(text.Substring(index, matchLen));
            if (HighlightRunStyle != null)
            {
                matchRun.Style = HighlightRunStyle;
            }
            else
            {
                // Back-compat default look
                matchRun.FontWeight = FontWeights.UltraBlack;
            }
            Inlines.Add(matchRun);

            // After match
            int afterIndex = index + matchLen;
            if (afterIndex < text.Length)
                Inlines.Add(new Run(text.Substring(afterIndex)));
        }

        #endregion
    }
}
