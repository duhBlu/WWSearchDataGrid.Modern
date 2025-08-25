using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WWSearchDataGrid.Modern.Core
{
    public class SearchTemplate : ObservableObject
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
                // Normalize to title case (e.g., "OR" -> "Or", "and" -> "And")
                var normalizedValue = string.IsNullOrEmpty(value) ? value : 
                    char.ToUpper(value[0]) + (value.Length > 1 ? value.Substring(1).ToLower() : string.Empty);
                
                if (SetProperty(normalizedValue, ref operatorName))
                {
                    if(normalizedValue?.ToLower() == "and")
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

        /// <summary>
        /// Gets the current search condition for this template
        /// </summary>
        public SearchCondition SearchCondition
        {
            get
            {
                // Create the same SearchCondition that would be used in BuildExpression
                var targetType = GetTargetType();
                return new SearchCondition(targetType, SearchType, SelectedValue, SelectedSecondaryValue);
            }
        }

        /// <summary>
        /// Gets the target type for the search condition
        /// </summary>
        private Type GetTargetType()
        {
            switch (ColumnDataType)
            {
                case ColumnDataType.DateTime:
                    return typeof(DateTime);
                case ColumnDataType.Number:
                    return typeof(decimal);
                case ColumnDataType.Boolean:
                    return typeof(bool);
                default:
                    return typeof(string);
            }
        }

        /// <summary>
        /// Gets available values from the parent controller (direct binding)
        /// </summary>
        public IEnumerable<object> AvailableValues => SearchTemplateController?.ColumnValues ?? Enumerable.Empty<object>();

        /// <summary>
        /// Gets the count of available values for display purposes
        /// </summary>
        public int AvailableValueCount => AvailableValues?.Count() ?? 0;

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

                var isNonDefaultSearchType = SearchTemplateController != null &&
                                                    SearchType != SearchTemplateController.DefaultSearchType;

                var hasSelectedValues = (SearchType == SearchType.IsAnyOf && SelectedValues.Any()) ||
                                      (SearchType == SearchType.IsNoneOf && SelectedValues.Any());
                var hasSelectedDates = SearchType == SearchType.IsOnAnyOfDates && SelectedDates.Any();
                var hasSelectedDateIntervals = SearchType == SearchType.DateInterval && DateIntervals.Any(i => i.IsSelected);
                
                // Check for non-value search types that are inherently valid
                var isNonValueSearchType = SearchType == SearchType.IsNull ||
                                         SearchType == SearchType.IsNotNull ||
                                         SearchType == SearchType.IsBlank ||
                                         SearchType == SearchType.IsNotBlank ||
                                         SearchType == SearchType.Yesterday ||
                                         SearchType == SearchType.Today ||
                                         SearchType == SearchType.AboveAverage ||
                                         SearchType == SearchType.BelowAverage ||
                                         SearchType == SearchType.Unique ||
                                         SearchType == SearchType.Duplicate;
                
                var result = hasSelectedValue || hasSelectedSecondaryValue || isNonDefaultSearchType ||
                           hasSelectedValues || hasSelectedDates || hasSelectedDateIntervals || isNonValueSearchType;
                
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
        private HashSet<object> _columnValuesForNullabilityAnalysis
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
                    addValueCommand = new RelayCommand(_ => SelectedValues.Add(null));
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
                        SelectedValues.Remove(value);
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
            _columnValuesForNullabilityAnalysis = new HashSet<object>();

            ColumnDataType = dataType;
            InitializeDateIntervals();
            UpdateInputTemplate();
        }

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
            if (_columnValuesForNullabilityAnalysis != null && _columnValuesForNullabilityAnalysis.Any())
            {
                isNullable = ReflectionHelper.IsNullableFromValues(_columnValuesForNullabilityAnalysis);
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

        private void EnsureOrderedForBetween()
        {
            if (SearchType == SearchType.Between
             || SearchType == SearchType.NotBetween
             || SearchType == SearchType.BetweenDates
             && Comparer.Default.Compare(SelectedValue, SelectedSecondaryValue) > 0)
            {
                (SelectedValue, SelectedSecondaryValue) =
                    (SelectedSecondaryValue, SelectedValue);
            }
        }

        /// <summary>
        /// Gets display text for a value (simplified version without count information)
        /// </summary>
        public string GetValueDisplayText(object value)
        {
            // All null, empty, and whitespace values display as "(null)"
            return value?.ToString() ?? "(null)";
        }

        #endregion

        #region Expression Builders

        public Expression<Func<object, bool>> BuildExpression(Type targetType)
        {
            if (SearchType == SearchType.IsAnyOf) return BuildIsAnyOfExpression();
            if (SearchType == SearchType.IsNoneOf) return BuildIsNoneOfExpression();
            if (SearchType == SearchType.IsOnAnyOfDates) return BuildIsOnAnyOfDatesExpression();
            if (SearchType == SearchType.DateInterval) return BuildDateIntervalExpression();

            EnsureOrderedForBetween();

            var searchCondition = new SearchCondition(targetType, SearchType, SelectedValue, SelectedSecondaryValue);
            return obj => SearchEngine.EvaluateCondition(obj, searchCondition);
        }

        private Expression<Func<object, bool>> BuildIsAnyOfExpression()
        {
            var values = SelectedValues.ToList();
            return obj => values.Contains(obj);
        }

        private Expression<Func<object, bool>> BuildIsNoneOfExpression()
        {
            var values = SelectedValues.ToList();
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
