using System.Windows.Input;

namespace WWControls.Wpf.Controls.Editors
{
    /// <summary>
    /// Routed commands for <see cref="WWPropertyGrid"/>. The item template raises
    /// <see cref="SelectItemCommand"/> to select a property row.
    /// </summary>
    public static class WWPropertyGridCommands
    {
        /// <summary>Selects the <see cref="WWPropertyItem"/> passed as the command parameter.</summary>
        public static readonly RoutedCommand SelectItemCommand =
            new RoutedCommand("SelectItem", typeof(WWPropertyGridCommands));
    }
}
