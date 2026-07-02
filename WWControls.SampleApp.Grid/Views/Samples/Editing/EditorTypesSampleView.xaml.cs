using System.ComponentModel;
using System.Windows.Controls;
using WWControls.Wpf;
using WWControls.Wpf.Editors.Settings;

namespace WWControls.SampleApp.Grid.Views.Samples.Editing
{
    public partial class EditorTypesSampleView : UserControl
    {
        public EditorTypesSampleView()
        {
            InitializeComponent();
            Loaded += (_, _) => HookViewModel();
        }

        /// <summary>
        /// Subscribes to the VM's <see cref="BooleanEditorKind"/> changes so the IsComplete column's
        /// <c>EditSettings</c> can be swapped out at runtime. The grid regenerates the column's
        /// internal WPF column when EditSettings changes via <see cref="GridColumn"/>'s
        /// <c>OnEditSettingsChanged</c>.
        /// </summary>
        private void HookViewModel()
        {
            if (DataContext is not EditorTypesSampleViewModel vm) return;
            vm.PropertyChanged += OnVmPropertyChanged;
            ApplyBooleanEditor(vm.BooleanEditor, vm);
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not EditorTypesSampleViewModel vm) return;
            if (e.PropertyName == nameof(EditorTypesSampleViewModel.BooleanEditor))
                ApplyBooleanEditor(vm.BooleanEditor, vm);
        }

        private void ApplyBooleanEditor(BooleanEditorKind kind, EditorTypesSampleViewModel vm)
        {
            DoneColumn.EditSettings = kind switch
            {
                BooleanEditorKind.TextBox => new TextBoxSettings(),
                BooleanEditorKind.ComboBox => new ComboBoxSettings { ItemsSource = vm.BooleanOptions },
                _ => new CheckBoxSettings(),
            };
        }
    }
}
