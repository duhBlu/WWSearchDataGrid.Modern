using System.ComponentModel;
using System.Runtime.CompilerServices;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// One column row in the View Totals editor's Items tab. The four function checkboxes
    /// read/write the owning view-model's working entry set; Can* gates each function by the
    /// column's <see cref="ColumnDataBase.FieldType"/> (Average/Sum disable on non-numeric
    /// columns, matching the dialog's greyed checkboxes).
    /// </summary>
    public sealed class SummaryEditorColumnItem : INotifyPropertyChanged
    {
        private readonly GroupSummaryEditorViewModel _owner;

        internal SummaryEditorColumnItem(GroupSummaryEditorViewModel owner, GridColumn column)
        {
            _owner = owner;
            Column = column;
        }

        public GridColumn Column { get; }

        public string Caption
        {
            get
            {
                string caption = Column.HeaderCaption;
                return string.IsNullOrEmpty(caption) ? Column.FieldName : caption;
            }
        }

        /// <summary>
        /// True when any configured entry in the working copy targets this column — rendered
        /// bold in the Items tab's column list.
        /// </summary>
        public bool HasConfiguredSummaries => _owner.HasAnyEntryFor(Column);

        /// <summary>Re-evaluates <see cref="HasConfiguredSummaries"/> after entry membership changes.</summary>
        internal void NotifyConfigurationChanged() => OnPropertyChanged(nameof(HasConfiguredSummaries));

        public bool CanMax => SummaryCalculator.IsTypeSupported(SummaryItemType.Max, Column.FieldType);
        public bool CanMin => SummaryCalculator.IsTypeSupported(SummaryItemType.Min, Column.FieldType);
        public bool CanAverage => SummaryCalculator.IsTypeSupported(SummaryItemType.Average, Column.FieldType);
        public bool CanSum => SummaryCalculator.IsTypeSupported(SummaryItemType.Sum, Column.FieldType);

        public bool IsMaxChecked
        {
            get => _owner.HasEntry(Column, SummaryItemType.Max);
            set { _owner.SetEntry(Column, SummaryItemType.Max, value); OnPropertyChanged(); }
        }

        public bool IsMinChecked
        {
            get => _owner.HasEntry(Column, SummaryItemType.Min);
            set { _owner.SetEntry(Column, SummaryItemType.Min, value); OnPropertyChanged(); }
        }

        public bool IsAverageChecked
        {
            get => _owner.HasEntry(Column, SummaryItemType.Average);
            set { _owner.SetEntry(Column, SummaryItemType.Average, value); OnPropertyChanged(); }
        }

        public bool IsSumChecked
        {
            get => _owner.HasEntry(Column, SummaryItemType.Sum);
            set { _owner.SetEntry(Column, SummaryItemType.Sum, value); OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
