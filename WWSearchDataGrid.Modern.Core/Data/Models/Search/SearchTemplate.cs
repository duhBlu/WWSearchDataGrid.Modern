using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Input;

namespace WWSearchDataGrid.Modern.Core
{
    public class SearchTemplate : ObservableObject, ILogicalOperatorProvider
    {
        #region Fields

        private string operatorName = "Or";
        private Func<Expression, Expression, Expression> operatorFunction = Expression.Or;
        private SearchType searchType = SearchType.Contains;
        private bool isOperatorVisible = true;
        private object selectedValue;
        private object selectedSecondaryValue;
        private SearchTemplateController searchTemplateController;

        private ColumnDataType columnDataType;
        private FilterInputTemplate inputTemplate;
        private ObservableCollection<object> selectedValues;
        private ObservableCollection<DateTime> selectedDates;
        private ObservableCollection<DateIntervalItem> dateIntervals;
        private HashSet<object> columnValues;

        private ICommand addValueCommand;
        private ICommand removeValueCommand;
        private ICommand addDateCommand;
        private ICommand removeDateCommand;

        #endregion

        #region Common Properties

        public Func<Expression, Expression, Expression> OperatorFunction
        {
            get { return operatorFunction; }
            set { SetProperty(value, ref operatorFunction); }
        }

        public string OperatorName
        {
            get { return operatorName; }
            set
            {
                if (SetProperty(value, ref operatorName))
                {
                    if(value == "And")
                    {
                        OperatorFunction = Expression.And;
                    }
                    else
                    {
                        OperatorFunction = Expression.Or;
                    }
                }
            }
        }

        public SearchType SearchType
        {
            get { return searchType; }
            set
            {
                if (SetProperty(value, ref searchType))
                {
                    HasChanges = true;
                    OnSearchTypeChanged();
                }
            }
        }

        public bool HasChanges { get; set; }

        public bool IsOperatorVisible
        {
            get { return isOperatorVisible; }
            set { SetProperty(value, ref isOperatorVisible); }
        }

        public HashSet<object> AvailableValues { get; set; } = new HashSet<object>();

        public object SelectedValue
        {
            get { return selectedValue; }
            set
            {
                if (SetProperty(value, ref selectedValue))
                {
                    HasChanges = true;
                }
            }
        }

        public object SelectedSecondaryValue
        {
            get { return selectedSecondaryValue; }
            set
            {
                if (SetProperty(value, ref selectedSecondaryValue))
                {
                    HasChanges = true;
                }
            }
        }

        public SearchTemplateController SearchTemplateController
        {
            get { return searchTemplateController; }
            set { SetProperty(value, ref searchTemplateController); }
        }

        public bool HasCustomFilter
        {
            get
            {
                var hasSelectedValue = SelectedValue != null;
                var hasSelectedSecondaryValue = SelectedSecondaryValue != null;
                var isNonDefaultSearchType = SearchType != SearchType.Contains;
                var hasSelectedValues = (SearchType == SearchType.IsAnyOf && SelectedValues.Any()) ||
                                      (SearchType == SearchType.IsNoneOf && SelectedValues.Any());
                var hasSelectedDates = SearchType == SearchType.IsOnAnyOfDates && SelectedDates.Any();
                var hasSelectedDateIntervals = SearchType == SearchType.DateInterval && DateIntervals.Any(i => i.IsSelected);
                
                var result = hasSelectedValue || hasSelectedSecondaryValue || isNonDefaultSearchType || 
                           hasSelectedValues || hasSelectedDates || hasSelectedDateIntervals;
                
                return result;
            }
        }

        #endregion

        #region Specialized Properties

        public ColumnDataType ColumnDataType
        {
            get { return columnDataType; }
            set
            {
                if (SetProperty(value, ref columnDataType))
                {
                    UpdateValidSearchTypes();
                }
            }
        }

        public FilterInputTemplate InputTemplate
        {
            get { return inputTemplate; }
            private set { SetProperty(value, ref inputTemplate); }
        }

        public ObservableCollection<SearchType> ValidSearchTypes { get; private set; }

        /// <summary>
        /// Column values used for type analysis and nullability detection
        /// </summary>
        public HashSet<object> ColumnValues
        {
            get { return columnValues; }
            set 
            { 
                if (SetProperty(value, ref columnValues))
                {
                    UpdateValidSearchTypes();
                }
            }
        }

        public ObservableCollection<object> SelectedValues
        {
            get { return selectedValues; }
            set { SetProperty(value, ref selectedValues); }
        }

        public ObservableCollection<DateTime> SelectedDates
        {
            get { return selectedDates; }
            set { SetProperty(value, ref selectedDates); }
        }

        public ObservableCollection<DateIntervalItem> DateIntervals
        {
            get { return dateIntervals; }
            set { SetProperty(value, ref dateIntervals); }
        }

        #endregion

        #region Commands

        public ICommand AddValueCommand
        {
            get
            {
                if (addValueCommand == null)
                {
                    addValueCommand = new RelayCommand(_ => SelectedValues.Add(new FilterListValue { Value = null }));
                }
                return addValueCommand;
            }
        }

        public ICommand RemoveValueCommand
        {
            get
            {
                if (removeValueCommand == null)
                {
                    removeValueCommand = new RelayCommand(value =>
                    {
                        if (value is FilterListValue listValue)
                        {
                            SelectedValues.Remove(listValue);
                        }
                    });
                }
                return removeValueCommand;
            }
        }

        public ICommand AddDateCommand
        {
            get
            {
                if (addDateCommand == null)
                {
                    addDateCommand = new RelayCommand(_ => SelectedDates.Add(DateTime.Today));
                }
                return addDateCommand;
            }
        }

        public ICommand RemoveDateCommand
        {
            get
            {
                if (removeDateCommand == null)
                {
                    removeDateCommand = new RelayCommand(date =>
                    {
                        if (date is DateTime dt)
                        {
                            SelectedDates.Remove(dt);
                        }
                    });
                }
                return removeDateCommand;
            }
        }

        #endregion

        #region Constructors

        public SearchTemplate() : this(ColumnDataType.String) { }

        public SearchTemplate(ColumnDataType dataType)
        {
            ValidSearchTypes = new ObservableCollection<SearchType>();
            SelectedValues = new ObservableCollection<object>();
            SelectedDates = new ObservableCollection<DateTime>();
            DateIntervals = new ObservableCollection<DateIntervalItem>();
            ColumnValues = new HashSet<object>();

            ColumnDataType = dataType;
            InitializeDateIntervals();
            UpdateInputTemplate();
        }

        public SearchTemplate(HashSet<object> availableValues, ColumnDataType dataType)
            : this(dataType)
        {
            LoadAvailableValues(availableValues);
        }

        public SearchTemplate(HashSet<object> availableValues)
            : this(availableValues, ColumnDataType.String) { }

        #endregion

        #region Core Logic

        protected virtual void OnSearchTypeChanged()
        {
            UpdateInputTemplate();
        }

        private void UpdateInputTemplate()
        {
            var metadata = SearchTypeRegistry.GetMetadata(SearchType);
            if (metadata != null)
            {
                InputTemplate = metadata.InputTemplate;
            }
        }

        private void UpdateValidSearchTypes()
        {
            ValidSearchTypes.Clear();

            // Determine if the column type is nullable
            bool isNullable = true; // Default to nullable for backward compatibility
            if (ColumnValues != null && ColumnValues.Any())
            {
                isNullable = ReflectionHelper.IsNullableFromValues(ColumnValues);
            }

            var validTypes = SearchTypeRegistry.GetFiltersForDataType(ColumnDataType, isNullable);
            foreach (var filterType in validTypes)
            {
                ValidSearchTypes.Add(filterType.SearchType);
            }

            if (!ValidSearchTypes.Contains(SearchType) && ValidSearchTypes.Count > 0)
            {
                SearchType = ValidSearchTypes[0];
            }

            OnPropertyChanged(nameof(ValidSearchTypes));
        }

        private void InitializeDateIntervals()
        {
            DateIntervals.Clear();
            foreach (DateInterval interval in Enum.GetValues(typeof(DateInterval)))
            {
                var item = new DateIntervalItem
                {
                    Interval = interval,
                    DisplayName = GetDateIntervalDisplayName(interval),
                    IsSelected = false
                };
                DateIntervals.Add(item);
            }
        }

        private string GetDateIntervalDisplayName(DateInterval interval)
        {
            switch (interval)
            {
                case DateInterval.PriorThisYear: return "Prior this year";
                case DateInterval.EarlierThisYear: return "Earlier this year";
                case DateInterval.LaterThisYear: return "Later this year";
                case DateInterval.BeyondThisYear: return "Beyond this year";
                case DateInterval.EarlierThisMonth: return "Earlier this month";
                case DateInterval.LaterThisMonth: return "Later this month";
                case DateInterval.LastWeek: return "Last week";
                case DateInterval.NextWeek: return "Next week";
                case DateInterval.EarlierThisWeek: return "Earlier this week";
                case DateInterval.LaterThisWeek: return "Later this week";
                case DateInterval.Yesterday: return "Yesterday";
                case DateInterval.Today: return "Today";
                case DateInterval.Tomorrow: return "Tomorrow";
                default: return interval.ToString();
            }
        }

        public void LoadAvailableValues(HashSet<object> columnValues)
        {
            // Store the original column values for nullability analysis
            ColumnValues = columnValues;

            var newValues = new HashSet<object>();

            foreach (var v in columnValues.Where(v => v != null).OrderBy(v => v.ToStringEmptyIfNull()))
            {
                newValues.Add(v);
            }

            if (columnValues.Any(v => v == null))
            {
                newValues = new HashSet<object>(new object[] { null }.Concat(newValues));
            }

            AvailableValues = newValues;
            OnPropertyChanged(nameof(AvailableValues));

            if (columnValues.Any())
            {
                ColumnDataType = ReflectionHelper.DetermineColumnDataType(columnValues);
            }
        }

        #endregion

        #region Expression Builders

        public Expression<Func<object, bool>> BuildExpression(Type targetType)
        {
            if (SearchType == SearchType.IsAnyOf) return BuildIsAnyOfExpression();
            if (SearchType == SearchType.IsNoneOf) return BuildIsNoneOfExpression();
            if (SearchType == SearchType.IsOnAnyOfDates) return BuildIsOnAnyOfDatesExpression();
            if (SearchType == SearchType.DateInterval) return BuildDateIntervalExpression();

            var searchCondition = new SearchCondition(targetType, SearchType, SelectedValue, SelectedSecondaryValue);
            return obj => SearchEngine.EvaluateCondition(obj, searchCondition);
        }

        private Expression<Func<object, bool>> BuildIsAnyOfExpression()
        {
            var values = SelectedValues.Select(v => (v as FilterListValue)?.Value ?? v).ToList();
            return obj => values.Contains(obj);
        }

        private Expression<Func<object, bool>> BuildIsNoneOfExpression()
        {
            var values = SelectedValues.Select(v => (v as FilterListValue)?.Value ?? v).ToList();
            return obj => !values.Contains(obj);
        }

        private Expression<Func<object, bool>> BuildIsOnAnyOfDatesExpression()
        {
            var param = Expression.Parameter(typeof(object), "obj");
            var cast = Expression.Convert(param, typeof(DateTime));
            var prop = Expression.Property(cast, nameof(DateTime.Date));

            var dates = SelectedDates.Select(d => d.Date).ToList();
            var constDates = Expression.Constant(dates);

            var contains = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(DateTime));

            var call = Expression.Call(contains, constDates, prop);
            var isDateTime = Expression.TypeIs(param, typeof(DateTime));
            var body = Expression.Condition(isDateTime, call, Expression.Constant(false));

            return Expression.Lambda<Func<object, bool>>(body, param);
        }

        private Expression<Func<object, bool>> BuildDateIntervalExpression()
        {
            var param = Expression.Parameter(typeof(object), "obj");
            var isDateTime = Expression.TypeIs(param, typeof(DateTime));
            var dateVar = Expression.Variable(typeof(DateTime), "dateValue");
            var assign = Expression.Assign(dateVar, Expression.Convert(param, typeof(DateTime)));

            Expression combined = null;

            foreach (var interval in DateIntervals.Where(i => i.IsSelected).Select(i => i.Interval))
            {
                var condition = new SearchCondition(typeof(DateTime), SearchType.DateInterval, null, null, null, interval);
                var call = Expression.Call(
                    typeof(SearchEngine).GetMethod("EvaluateCondition", new[] { typeof(object), typeof(SearchCondition) }),
                    Expression.Convert(dateVar, typeof(object)),
                    Expression.Constant(condition)
                );

                if(combined == null)
                {
                    combined = call;
                }
                else
                {
                    combined = Expression.OrElse(combined, call);
                }
            }

            if (combined == null)
            {
                combined = Expression.Constant(false);
            }

            var block = Expression.Block(
                new[] { dateVar },
                Expression.Condition(isDateTime, Expression.Block(assign, combined), Expression.Constant(false))
            );

            return Expression.Lambda<Func<object, bool>>(block, param);
        }

        #endregion
    }
}
