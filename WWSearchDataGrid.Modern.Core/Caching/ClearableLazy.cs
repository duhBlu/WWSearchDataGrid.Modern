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
        /// Initializes a new instance of the ClearableLazy&lt;T&gt; class with the specified value factory
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
        /// Gets the lazily initialized value of the current ClearableLazy&lt;T&gt; instance
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
        
        /// <summary>
        /// Attempts to get the value without creating it if it hasn't been created yet
        /// </summary>
        /// <param name="value">The cached value if it exists, otherwise default(T)</param>
        /// <returns>True if the value was created and retrieved, false otherwise</returns>
        public bool TryGetValue(out T value)
        {
            if (_isValueCreated)
            {
                value = _value;
                return true;
            }
            
            value = default(T);
            return false;
        }
        
        /// <summary>
        /// Creates the value immediately if not already created
        /// </summary>
        /// <returns>The created value</returns>
        public T ForceValue()
        {
            return Value; // This will create the value if not already created
        }
        
        /// <summary>
        /// Provides a string representation of this instance
        /// </summary>
        /// <returns>A string representation indicating whether the value is created or not</returns>
        public override string ToString()
        {
            return IsValueCreated ? _value?.ToString() ?? "null" : "Value is not created.";
        }
    }
    
    /// <summary>
    /// Static factory methods for creating ClearableLazy instances
    /// </summary>
    internal static class ClearableLazy
    {
        /// <summary>
        /// Creates a new ClearableLazy&lt;T&gt; instance with the specified value factory
        /// </summary>
        /// <typeparam name="T">The type of value to lazily initialize</typeparam>
        /// <param name="valueFactory">The delegate that produces the value when needed</param>
        /// <returns>A new ClearableLazy&lt;T&gt; instance</returns>
        public static ClearableLazy<T> Create<T>(Func<T> valueFactory)
        {
            return new ClearableLazy<T>(valueFactory);
        }
        
        /// <summary>
        /// Creates a new ClearableLazy&lt;T&gt; instance with a constant value
        /// Useful for testing or when you have a value that should be wrapped in lazy semantics
        /// </summary>
        /// <typeparam name="T">The type of value to lazily initialize</typeparam>
        /// <param name="value">The constant value to wrap</param>
        /// <returns>A new ClearableLazy&lt;T&gt; instance</returns>
        public static ClearableLazy<T> FromValue<T>(T value)
        {
            return new ClearableLazy<T>(() => value);
        }
    }
}