using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using WWControls.Core;

namespace WWControls.Wpf
{
    /// <summary>
    /// Light-green chip that hosts the per-input-template value editor for a condition row.
    /// Display mode summarizes the value(s); editor mode swaps in whichever
    /// <see cref="FilterInputTemplate"/> editor the underlying <see cref="SearchTemplate"/>
    /// resolved to (single text box, dual text box, dual date picker, numeric up-down, etc.).
    /// </summary>
    public class ValueTokenEditor : EditableTokenBase
    {
        public static readonly DependencyProperty SearchTemplateProperty =
            DependencyProperty.Register(nameof(SearchTemplate), typeof(SearchTemplate), typeof(ValueTokenEditor),
                new PropertyMetadata(null, OnSearchTemplateChanged));

        public SearchTemplate SearchTemplate
        {
            get => (SearchTemplate)GetValue(SearchTemplateProperty);
            set => SetValue(SearchTemplateProperty, value);
        }

        public ValueTokenEditor()
        {
            DefaultStyleKey = typeof(ValueTokenEditor);
        }

        private static void OnSearchTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chip = (ValueTokenEditor)d;
            if (e.OldValue is SearchTemplate oldTpl)
            {
                oldTpl.PropertyChanged -= chip.OnTemplatePropertyChanged;
                chip.UnhookSelectedValues(oldTpl.SelectedValues);
            }
            if (e.NewValue is SearchTemplate newTpl)
            {
                newTpl.PropertyChanged += chip.OnTemplatePropertyChanged;
                chip.HookSelectedValues(newTpl.SelectedValues);
            }
            chip.RefreshDisplayText();
        }

        private void OnTemplatePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Any value-affecting change refreshes the chip summary.
            if (e.PropertyName == nameof(SearchTemplate.SelectedValue) ||
                e.PropertyName == nameof(SearchTemplate.SelectedSecondaryValue) ||
                e.PropertyName == nameof(SearchTemplate.SearchType) ||
                e.PropertyName == nameof(SearchTemplate.InputTemplate))
            {
                RefreshDisplayText();
            }
        }

        // The SelectedValues collection reference is stable across a SearchTemplate's lifetime
        // (constructed once, mutated in place), but rehooking on SearchTemplate swap keeps this
        // safe if that ever changes.
        private void HookSelectedValues(System.Collections.ObjectModel.ObservableCollection<SelectableValueItem> coll)
        {
            if (coll == null) return;
            coll.CollectionChanged += OnSelectedValuesCollectionChanged;
            foreach (var item in coll)
            {
                if (item != null) item.PropertyChanged += OnSelectedValueItemChanged;
            }
        }

        private void UnhookSelectedValues(System.Collections.ObjectModel.ObservableCollection<SelectableValueItem> coll)
        {
            if (coll == null) return;
            coll.CollectionChanged -= OnSelectedValuesCollectionChanged;
            foreach (var item in coll)
            {
                if (item != null) item.PropertyChanged -= OnSelectedValueItemChanged;
            }
        }

        private void OnSelectedValuesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<SelectableValueItem>())
                    item.PropertyChanged -= OnSelectedValueItemChanged;
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<SelectableValueItem>())
                    item.PropertyChanged += OnSelectedValueItemChanged;
            }
            RefreshDisplayText();
        }

        private void OnSelectedValueItemChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableValueItem.Value) ||
                e.PropertyName == nameof(SelectableValueItem.SelectedItem))
            {
                RefreshDisplayText();
            }
        }

        private void RefreshDisplayText()
        {
            DisplayText = ResolveDisplayText();
        }

        private string ResolveDisplayText()
        {
            var tpl = SearchTemplate;
            if (tpl == null) return "(value)";

            switch (tpl.SearchType)
            {
                case SearchType.Between:
                case SearchType.NotBetween:
                case SearchType.BetweenDates:
                case SearchType.NotBetweenDates:
                    return $"{Format(tpl.SelectedValue)} and {Format(tpl.SelectedSecondaryValue)}";

                case SearchType.IsNull:
                case SearchType.IsNotNull:
                case SearchType.Yesterday:
                case SearchType.Today:
                case SearchType.AboveAverage:
                case SearchType.BelowAverage:
                case SearchType.Unique:
                case SearchType.Duplicate:
                    return string.Empty;

                case SearchType.IsAnyOf:
                case SearchType.IsNoneOf:
                {
                    var items = tpl.SelectedValues?.Where(v => v != null && !string.IsNullOrEmpty(v.Value))
                                                  .Select(v => v.Value)
                                                  .Take(3)
                                                  .ToList();
                    if (items == null || items.Count == 0) return "(none)";
                    var moreCount = (tpl.SelectedValues?.Count ?? 0) - items.Count;
                    var sb = new StringBuilder(string.Join(", ", items));
                    if (moreCount > 0) sb.Append($", +{moreCount} more");
                    return sb.ToString();
                }

                case SearchType.IsOnAnyOfDates:
                {
                    var dates = tpl.SelectedDates?.Take(3).ToList();
                    if (dates == null || dates.Count == 0) return "(none)";
                    var moreCount = (tpl.SelectedDates?.Count ?? 0) - dates.Count;
                    var sb = new StringBuilder(string.Join(", ", dates.Select(d => d.ToShortDateString())));
                    if (moreCount > 0) sb.Append($", +{moreCount} more");
                    return sb.ToString();
                }

                case SearchType.DateInterval:
                {
                    var intervals = tpl.DateIntervals?.Where(i => i.IsSelected).Select(i => i.DisplayName).Take(3).ToList();
                    if (intervals == null || intervals.Count == 0) return "(none)";
                    return string.Join(", ", intervals);
                }

                default:
                    return Format(tpl.SelectedValue);
            }
        }

        private static string Format(object value)
        {
            if (value == null) return "(empty)";
            if (value is DateTime dt) return dt.ToShortDateString();
            var s = value.ToString();
            return string.IsNullOrEmpty(s) ? "(empty)" : s;
        }
    }
}
