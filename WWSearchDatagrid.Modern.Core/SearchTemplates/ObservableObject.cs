using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Base implementation of INotifyPropertyChanged for model classes
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for the specified property
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the field value and raises PropertyChanged if value has changed
        /// </summary>
        /// <typeparam name="T">Type of the field</typeparam>
        /// <param name="value">New value</param>
        /// <param name="field">Reference to the backing field</param>
        /// <param name="propertyName">Name of the property</param>
        /// <returns>True if value was changed, false otherwise</returns>
        protected bool SetProperty<T>(T value, ref T field, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Sets the field value, raises PropertyChanged, and executes callback if value has changed
        /// </summary>
        /// <typeparam name="T">Type of the field</typeparam>
        /// <param name="value">New value</param>
        /// <param name="field">Reference to the backing field</param>
        /// <param name="onChanged">Action to execute after property change</param>
        /// <param name="propertyName">Name of the property</param>
        /// <returns>True if value was changed, false otherwise</returns>
        protected bool SetProperty<T>(T value, ref T field, Action onChanged, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer.Default.Equals(field, value))
                return false;

            field = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    /// <summary>
    /// Helper class for equality comparison
    /// </summary>
    internal static class EqualityComparer
    {
        public static IEqualityComparer<object> Default { get; } = new ObjectEqualityComparer();

        private class ObjectEqualityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null || y == null)
                    return false;

                return x.Equals(y);
            }

            public int GetHashCode(object obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}
