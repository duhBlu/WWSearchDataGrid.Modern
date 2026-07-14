namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Which selection glyph <see cref="WWListBox"/> rows render. Purely visual — how items
    /// select (click-toggle vs Ctrl/Shift) is governed by the ListBox's native
    /// <see cref="System.Windows.Controls.ListBox.SelectionMode"/>.
    /// </summary>
    public enum ListBoxItemKind
    {
        /// <summary>No glyph — selection reads as the row highlight alone.</summary>
        Default,

        /// <summary>Each row shows a checkbox lit by IsSelected. Pairs naturally with SelectionMode=Multiple.</summary>
        Checked,

        /// <summary>Each row shows a radio dot lit by IsSelected. Pairs naturally with SelectionMode=Single.</summary>
        Radio,
    }
}
