using System;
using System.Threading;

namespace WWSearchDataGrid.Modern.Core.Caching
{
    /// <summary>
    /// A thread-safe lazy initialization wrapper that allows clearing cached values
    /// </summary>
    /// <typeparam name="T">The type of value to lazily initialize</typeparam>
    internal class ClearableLazy<T>
    {
        private readonly Func<T> _valueFactory;
        private readonly object _lock = new object();
        private volatile bool _isValueCreated;
        private T _value;
        
        /// <summary>
        /// Initializes a new instance of the ClearableLazy<T> class with the specified value factory
        /// </summary>
        /// <param name="valueFactory">The delegate that produces the value when needed</param>
        /// <exception cref="ArgumentNullException">valueFactory is null</exception>
        public ClearableLazy(Func<T> valueFactory)
        {
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
        }
        
        /// <summary>
        /// Gets a value that indicates whether this instance may produce a value without blocking
        /// </summary>
        public bool IsValueCreated => _isValueCreated;
        
        /// <summary>
        /// Gets the lazily initialized value of the current ClearableLazy<T> instance
        /// </summary>
        public T Value
        {
            get
            {
                if (!_isValueCreated)
                {
                    lock (_lock)
                    {
                        if (!_isValueCreated)
                        {
                            try
                            {
                                _value = _valueFactory();
                                _isValueCreated = true;
                            }
                            catch (Exception)
                            {
                                // Ensure we don't cache failed attempts
                                _isValueCreated = false;
                                _value = default(T);
                                throw;
                            }
                        }
                    }
                }
                return _value;
            }
        }
        
        /// <summary>
        /// Clears the cached value, allowing it to be garbage collected
        /// The next access to Value will re-execute the value factory
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                if (_isValueCreated)
                {
                    // Clear the reference to allow GC
                    _value = default(T);
                    _isValueCreated = false;
                }
            }
        }
    }
}