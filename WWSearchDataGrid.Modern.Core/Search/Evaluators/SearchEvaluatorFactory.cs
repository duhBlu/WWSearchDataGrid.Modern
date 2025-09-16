using System;
using System.Collections.Generic;
using System.Linq;

namespace WWSearchDataGrid.Modern.Core.Strategies
{
    /// <summary>
    /// Factory for creating and managing search evaluators
    /// </summary>
    internal class SearchEvaluatorFactory
    {
        private static readonly Lazy<SearchEvaluatorFactory> _instance = 
            new Lazy<SearchEvaluatorFactory>(() => new SearchEvaluatorFactory());

        private readonly Dictionary<SearchType, ISearchEvaluator> _evaluators;
        private readonly List<ISearchEvaluator> _allEvaluators;

        /// <summary>
        /// Singleton instance of the factory
        /// </summary>
        public static SearchEvaluatorFactory Instance => _instance.Value;

        /// <summary>
        /// Private constructor for singleton pattern
        /// </summary>
        private SearchEvaluatorFactory()
        {
            _allEvaluators = new List<ISearchEvaluator>();
            _evaluators = new Dictionary<SearchType, ISearchEvaluator>();
            RegisterDefaultEvaluators();
        }

        /// <summary>
        /// Gets an evaluator for the specified search type
        /// </summary>
        /// <param name="searchType">Search type to get evaluator for</param>
        /// <returns>Evaluator instance or null if not found</returns>
        public ISearchEvaluator GetEvaluator(SearchType searchType)
        {
            if (_evaluators.TryGetValue(searchType, out var evaluator))
            {
                return evaluator;
            }

            // Try to find an evaluator that can handle this search type
            evaluator = _allEvaluators.FirstOrDefault(e => e.CanHandle(searchType));
            if (evaluator != null)
            {
                _evaluators[searchType] = evaluator; // Cache for future use
            }

            return evaluator;
        }

        /// <summary>
        /// Registers a custom evaluator
        /// </summary>
        /// <param name="evaluator">Evaluator to register</param>
        public void RegisterEvaluator(ISearchEvaluator evaluator)
        {
            if (evaluator == null)
                throw new ArgumentNullException(nameof(evaluator));

            _allEvaluators.Add(evaluator);
            _evaluators[evaluator.SearchType] = evaluator;

            // Re-sort by priority (higher priority first)
            _allEvaluators.Sort((x, y) => y.Priority.CompareTo(x.Priority));
        }

        /// <summary>
        /// Registers all default evaluators
        /// </summary>
        private void RegisterDefaultEvaluators()
        {
            // Text evaluators
            RegisterEvaluator(new ContainsEvaluator());
            RegisterEvaluator(new DoesNotContainEvaluator());
            RegisterEvaluator(new StartsWithEvaluator());
            RegisterEvaluator(new EndsWithEvaluator());
            RegisterEvaluator(new EqualsEvaluator());
            RegisterEvaluator(new NotEqualsEvaluator());

            // Comparison evaluators
            RegisterEvaluator(new LessThanEvaluator());
            RegisterEvaluator(new LessThanOrEqualToEvaluator());
            RegisterEvaluator(new GreaterThanEvaluator());
            RegisterEvaluator(new GreaterThanOrEqualToEvaluator());

            // Range evaluators
            RegisterEvaluator(new BetweenEvaluator());
            RegisterEvaluator(new NotBetweenEvaluator());
            RegisterEvaluator(new BetweenDatesEvaluator());

            // Null evaluators
            RegisterEvaluator(new IsNullEvaluator());
            RegisterEvaluator(new IsNotNullEvaluator());

            // Pattern evaluators
            RegisterEvaluator(new IsLikeEvaluator());
            RegisterEvaluator(new IsNotLikeEvaluator());

            // Date evaluators
            RegisterEvaluator(new DateIntervalEvaluator());
            RegisterEvaluator(new YesterdayEvaluator());
            RegisterEvaluator(new TodayEvaluator());

            // Multi-value evaluators
            RegisterEvaluator(new IsAnyOfEvaluator());
            RegisterEvaluator(new IsNoneOfEvaluator());
            RegisterEvaluator(new IsOnAnyOfDatesEvaluator());

            // Collection context evaluators
            RegisterEvaluator(new TopNEvaluator());
            RegisterEvaluator(new BottomNEvaluator());
            RegisterEvaluator(new AboveAverageEvaluator());
            RegisterEvaluator(new BelowAverageEvaluator());
            RegisterEvaluator(new UniqueEvaluator());
            RegisterEvaluator(new DuplicateEvaluator());
        }
    }
}