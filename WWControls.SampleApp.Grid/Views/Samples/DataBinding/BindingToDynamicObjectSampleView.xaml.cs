using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WWControls.Wpf;
using WWControls.Wpf.Controls.Editors.Settings;

namespace WWControls.SampleApp.Grid.Views.Samples.DataBinding
{
    public partial class BindingToDynamicObjectSampleView : UserControl
    {
        public BindingToDynamicObjectSampleView() => InitializeComponent();

        // Column-adding needs the grid instance, so it lives in the code-behind (mirroring the
        // classic "create new column" pattern). The view-model registers the field + back-fills
        // the rows; here we add the matching GridColumn with an explicit Binding to that field —
        // exactly how a runtime column over a dynamic source is wired.
        private void OnAddColumnClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not BindingToDynamicObjectSampleViewModel vm)
                return;

            var spec = vm.AddDynamicColumn();
            var column = new GridColumn
            {
                Header = spec.FieldName,
                Binding = new Binding(spec.FieldName) { Mode = BindingMode.TwoWay },
            };

            // Per-column editor type: set EditSettings on the descriptor. An explicit EditSettings
            // wins over the CLR-type auto-default, so a dynamic string column edits through a
            // ComboBox bound to the supplied choices (the selected value writes back to the
            // ExpandoObject member via the column's Binding).
            if (spec.ComboChoices != null)
                column.EditSettings = new ComboBoxSettings { ItemsSource = spec.ComboChoices };

            DynamicGrid.GridColumns.Add(column);
        }
    }
}
