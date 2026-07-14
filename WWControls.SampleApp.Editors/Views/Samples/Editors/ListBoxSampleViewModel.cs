using System.Collections.ObjectModel;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using WWControls.Wpf.Editors;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the WWListBox playground: one demo list whose option surface — SelectionMode,
    /// ItemKind, and the built-in reorder feature — is driven live from the options panel.
    /// The view's code-behind feeds <see cref="SelectionSummary"/> from SelectionChanged and
    /// applies ItemReordered moves back onto <see cref="Items"/>.
    /// </summary>
    public partial class ListBoxSampleViewModel : ObservableObject
    {
        public ObservableCollection<string> Items { get; } = new()
        {
            "Base Cabinet",
            "Wall Cabinet",
            "Tall Pantry",
            "Corner Lazy Susan",
            "Drawer Bank",
            "Sink Base",
            "Oven Cabinet",
            "Refrigerator Panel",
            "Open Shelf Unit",
            "Wine Rack",
        };

        [ObservableProperty]
        private SelectionMode _selectionMode = SelectionMode.Single;

        [ObservableProperty]
        private ListBoxItemKind _itemKind = ListBoxItemKind.Default;

        [ObservableProperty]
        private bool _allowReorder;

        [ObservableProperty]
        private int _reorderAnimationDuration = 200;

        [ObservableProperty]
        private string _selectionSummary = "Selected: (none)";

        [ObservableProperty]
        private string _lastReorder = "Last reorder: (none yet)";

        public IReadOnlyList<SelectionMode> SelectionModes { get; } =
            new[] { SelectionMode.Single, SelectionMode.Multiple, SelectionMode.Extended };

        public IReadOnlyList<ListBoxItemKind> ItemKinds { get; } =
            new[] { ListBoxItemKind.Default, ListBoxItemKind.Checked, ListBoxItemKind.Radio };
    }
}
