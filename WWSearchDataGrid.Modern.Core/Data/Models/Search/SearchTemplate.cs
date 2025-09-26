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
                // Normalize to title case for display text (e.g., "OR" -> "Or", "and" -> "And")
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
                    UpdateInputTemplate();
                }
            }
        }

        public bool HasChanges { get; set; }

        public bool IsOperatorVisible
        {
            get { return isOperatorVisible; }
            set { SetProperty(value, ref isOperatorVisible); }
        }

        public SearchCondition SearchCondition
        {
            get
            {
                // Create the same SearchCondition that would be used in BuildExpression
                var targetType = GetTargetType();
                return new SearchCondition(targetType, SearchType, SelectedValue, SelectedSecondaryValue);
            }
        }

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

                    OnPropertyChanged(nameof(HasCustomFilter));
                    OnPropertyChanged(nameof(IsValidFilter));
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
                    OnPropertyChanged(nameof(HasCustomFilter));
                    OnPropertyChanged(nameof(IsValidFilter));
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
                var hasSelectedValue = SelectedValue != null && !string.IsNullOrEmpty(SelectedValue.ToString());
                var hasSelectedSecondaryValue = SelectedSecondaryValue != null && !string.IsNullOrEmpty(SelectedValue.ToString());

                var isNonDefaultSearchType = SearchType != SearchType.Contains;

                var hasSelectedValues = (SearchType == SearchType.IsAnyOf && SelectedValues.Any()) ||
                                      (SearchType == SearchType.IsNoneOf && SelectedValues.Any());
                var hasSelectedDates = SearchType == SearchType.IsOnAnyOfDates && SelectedDates.Any();
                var hasSelectedDateIntervals = SearchType == SearchType.DateInterval && DateIntervals.Any(i => i.IsSelected);
                
                // Check for non-value search types that are inherently valid
                var isNonValueSearchType = SearchType == SearchType.IsNull ||
                                         SearchType == SearchType.IsNotNull ||
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

        /// <summary>
        /// Gets whether this search template represents a complete and valid filter that can be applied
        /// </summary>
        public bool IsValidFilter
        {
            get
            {
                // Non-value search types that don't require input values
                var isNonValueSearchType = SearchType == SearchType.IsNull ||
                                         SearchType == SearchType.IsNotNull ||
                                         SearchType == SearchType.Yesterday ||
                                         SearchType == SearchType.Today ||
                                         SearchType == SearchType.AboveAverage ||
                                         SearchType == SearchType.BelowAverage ||
                                         SearchType == SearchType.Unique ||
                                         SearchType == SearchType.Duplicate;

                if (isNonValueSearchType)
                {
                    return true; // These are always valid
                }

                // Search types that require a single value
                var singleValueSearchTypes = new[]
                {
                    SearchType.Contains, SearchType.DoesNotContain, SearchType.StartsWith, SearchType.EndsWith,
                    SearchType.Equals, SearchType.NotEquals, SearchType.LessThan, SearchType.LessThanOrEqualTo,
                    SearchType.GreaterThan, SearchType.GreaterThanOrEqualTo, SearchType.TopN, SearchType.BottomN
                };

                if (singleValueSearchTypes.Contains(SearchType))
                {
                    return SelectedValue != null && !string.IsNullOrWhiteSpace(SelectedValue?.ToString());
                }

                // Search types that require two values (Between, NotBetween, BetweenDates)
                if (SearchType == SearchType.Between || SearchType == SearchType.NotBetween || SearchType == SearchType.BetweenDates)
                {
                    return SelectedValue != null && SelectedSecondaryValue != null &&
                           !string.IsNullOrWhiteSpace(SelectedValue?.ToString()) &&
                           !string.IsNullOrWhiteSpace(SelectedSecondaryValue?.ToString());
                }

                // Search types that require collection values
                if (SearchType == SearchType.IsAnyOf || SearchType == SearchType.IsNoneOf)
                {
                    return SelectedValues != null && SelectedValues.Any();
                }

                if (SearchType == SearchType.IsOnAnyOfDates)
                {
                    return SelectedDates != null && SelectedDates.Any();
                }

                if (SearchType == SearchType.DateInterval)
                {
                    return DateIntervals != null && DateIntervals.Any(i => i.IsSelected);
                }

                // Default: single value search types
                return SelectedValue != null && !string.IsNullOrWhiteSpace(SelectedValue?.ToString());
            }
        }

        #endregion

        #region Specialized Properties

        public ColumnDataType ColumnDataType
        {
            get { return columnDataType; }
            set
            {
                SetProperty(value, ref columnDataType);
                UpdateValidSearchTypes();
            }
        }

        public FilterInputTemplate InputTemplate
        {
            get { return inputTemplate; }
            private set { SetProperty(value, ref inputTemplate); }
        }

        public ObservableCollection<SearchType> ValidSearchTypes { get; private set; }


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

            ColumnDataType = dataType;
            InitializeDateIntervals();
            UpdateInputTemplate();
        }

        #endregion

        #region Core Logic
        

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
            // Use the controller's nullability detection instead of local analysis
            bool isNullable = SearchTemplateController?.ContainsNullValues ?? false;
            var validTypes = SearchTypeRegistry.GetFiltersForDataType(ColumnDataType, isNullable);
            var newValidSearchTypes = validTypes.Select(filterType => filterType.SearchType);

            // Remove invalid search types (ones that are no longer valid)
            var itemsToRemove = ValidSearchTypes.Where(searchType => !newValidSearchTypes.Contains(searchType)).ToList();
            foreach (var item in itemsToRemove)
            {
                ValidSearchTypes.Remove(item);
            }

            // Add new valid search types that don't already exist
            foreach (var searchType in newValidSearchTypes)
            {
                if (!ValidSearchTypes.Contains(searchType))
                {
                    ValidSearchTypes.Add(searchType);
                }
            }

            // If current SearchType is no longer valid and we have valid options, set to first valid option
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

        /// <summary>
        /// Gets the ordered values for Between operations without modifying the stored values
        /// This prevents infinite loops (property changed listeners) while ensuring correct ordering for comparisons
        /// </summary>
        private (object minValue, object maxValue) GetOrderedBetweenValues()
        {
            // Check if this is a Between operation that needs ordering
            if ((SearchType == SearchType.Between ||
                 SearchType == SearchType.NotBetween ||
                 SearchType == SearchType.BetweenDates) &&
                SelectedValue != null &&
                SelectedSecondaryValue != null &&
                Comparer.Default.Compare(SelectedValue, SelectedSecondaryValue) > 0)
            {
                // Return values in correct order (min, max) without modifying stored properties
                return (SelectedSecondaryValue, SelectedValue);
            }

            // Return original values if no ordering needed
            return (SelectedValue, SelectedSecondaryValue);
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

        #region Value Removal Methods

        /// <summary>
        /// Removes the primary value from this template
        /// </summary>
        /// <returns>True if the template is still valid after removal, false if it should be removed</returns>
        public bool RemovePrimaryValue()
        {
            SelectedValue = null;
            HasChanges = true;

            // Check if template is still valid
            return IsValidFilter;
        }

        /// <summary>
        /// Removes the secondary value from this template
        /// </summary>
        /// <returns>True if the template is still valid after removal, false if it should be removed</returns>
        public bool RemoveSecondaryValue()
        {
            SelectedSecondaryValue = null;
            HasChanges = true;

            // For Between/Range templates, transform to single value template
            if (SearchType == SearchType.Between && SelectedValue != null)
            {
                SearchType = SearchType.GreaterThanOrEqualTo;
                return true;
            }
            else if (SearchType == SearchType.NotBetween && SelectedValue != null)
            {
                SearchType = SearchType.NotEquals;
                return true;
            }
            else if (SearchType == SearchType.BetweenDates && SelectedValue != null)
            {
                SearchType = SearchType.GreaterThanOrEqualTo;
                return true;
            }

            // Check if template is still valid
            return IsValidFilter;
        }

        /// <summary>
        /// Removes a specific value from the SelectedValues collection
        /// </summary>
        /// <param name="value">The value to remove</param>
        /// <returns>True if the template is still valid after removal, false if it should be removed</returns>
        public bool RemoveSelectedValue(object value)
        {
            if (SelectedValues != null && SelectedValues.Contains(value))
            {
                SelectedValues.Remove(value);
                HasChanges = true;
            }

            // Check if template is still valid
            return IsValidFilter;
        }

        /// <summary>
        /// Removes a specific date from the SelectedDates collection
        /// </summary>
        /// <param name="date">The date to remove</param>
        /// <returns>True if the template is still valid after removal, false if it should be removed</returns>
        public bool RemoveSelectedDate(DateTime date)
        {
            if (SelectedDates != null && SelectedDates.Contains(date))
            {
                SelectedDates.Remove(date);
                HasChanges = true;
            }

            // Check if template is still valid
            return IsValidFilter;
        }

        /// <summary>
        /// Checks if the template would be valid after value removal without actually performing the removal
        /// </summary>
        /// <param name="context">The removal context containing removal information</param>
        /// <returns>True if the template would be valid after removal, false if it would become invalid</returns>
        public bool WouldBeValidAfterValueRemoval(ValueRemovalContext context)
        {
            switch (context.ValueType)
            {
                case ValueType.Primary:
                    // For primary value removal, template would be invalid if there's no secondary value
                    return SelectedSecondaryValue != null || (SelectedValues?.Count > 1) || (SelectedDates?.Count > 0);

                case ValueType.Secondary:
                    // For secondary value removal, template would be invalid if there's no primary value
                    return SelectedValue != null || (SelectedValues?.Count > 0) || (SelectedDates?.Count > 0);

                case ValueType.CollectionItem:
                    // For collection item removal, template would be invalid if this is the last item
                    return (SelectedValues?.Count > 1);

                case ValueType.DateItem:
                    // For date item removal, template would be invalid if this is the last date
                    return (SelectedDates?.Count > 1);

                case ValueType.UnarySearchType:
                    // Unary search types don't have values, so removing them always makes the template invalid
                    return false;
            }

            // Default to checking current validity
            return IsValidFilter;
        }

        /// <summary>
        /// Handles value removal based on the removal context
        /// </summary>
        /// <param name="context">The removal context containing removal information</param>
        /// <returns>True if the template is still valid after removal, false if it should be removed</returns>
        public bool HandleValueRemoval(ValueRemovalContext context)
        {
            switch (context.ValueType)
            {
                case ValueType.Primary:
                    return RemovePrimaryValue();

                case ValueType.Secondary:
                    return RemoveSecondaryValue();

                case ValueType.CollectionItem:
                    return RemoveSelectedValue(context.OriginalValue);

                case ValueType.DateItem:
                    if (context.OriginalValue is DateTime date)
                        return RemoveSelectedDate(date);
                    break;

                case ValueType.UnarySearchType:
                    return RemoveUnarySearchType();
            }

            return IsValidFilter;
        }

        /// <summary>
        /// Removes a UnarySearchType template (templates that don't require input values)
        /// </summary>
        /// <returns>False since UnarySearchType templates should be completely removed</returns>
        public bool RemoveUnarySearchType()
        {
            // UnarySearchType templates like IsNull, AboveAverage, Today, etc.
            // should be completely removed since they don't have individual values
            HasChanges = true;
            return false; // Signal that the entire template should be removed
        }

        /// <summary>
        /// Transforms Between search types to single-value equivalents when one value is removed
        /// </summary>
        /// <param name="removedValueType">The type of value that was removed</param>
        public void TransformBetweenSearchType(ValueType removedValueType)
        {
            if (SearchType == SearchType.Between)
            {
                if (removedValueType == ValueType.Primary && SelectedSecondaryValue != null)
                {
                    SearchType = SearchType.LessThanOrEqualTo;
                }
                else if (removedValueType == ValueType.Secondary && SelectedValue != null)
                {
                    SearchType = SearchType.GreaterThanOrEqualTo;
                }
            }
            else if (SearchType == SearchType.NotBetween)
            {
                if (removedValueType == ValueType.Primary && SelectedSecondaryValue != null)
                {
                    SearchType = SearchType.GreaterThan;
                }
                else if (removedValueType == ValueType.Secondary && SelectedValue != null)
                {
                    SearchType = SearchType.LessThan;
                }
            }
            else if (SearchType == SearchType.BetweenDates)
            {
                if (removedValueType == ValueType.Primary && SelectedSecondaryValue != null)
                {
                    SearchType = SearchType.LessThanOrEqualTo;
                }
                else if (removedValueType == ValueType.Secondary && SelectedValue != null)
                {
                    SearchType = SearchType.GreaterThanOrEqualTo;
                }
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

            var (orderedValue, orderedSecondaryValue) = GetOrderedBetweenValues(); 

            var searchCondition = new SearchCondition(targetType, SearchType, orderedValue, orderedSecondaryValue);
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
