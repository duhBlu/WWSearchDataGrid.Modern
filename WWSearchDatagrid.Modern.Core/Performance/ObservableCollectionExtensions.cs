using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace WWSearchDataGrid.Modern.Core.Performance
{
    /// <summary>
    /// Extension to support binary search on ObservableCollection
    /// </summary>
    public static class ObservableCollectionExtensions
    {
        public static int BinarySearch<T>(this ObservableCollection<T> collection, T item, IComparer<T> comparer)
        {
            return BinarySearch(collection, 0, collection.Count, item, comparer);
        }

        private static int BinarySearch<T>(ObservableCollection<T> collection, int index, int length, T value, IComparer<T> comparer)
        {
            int low = index;
            int high = index + length - 1;

            while (low <= high)
            {
                int mid = low + ((high - low) >> 1);
                int comp = comparer.Compare(collection[mid], value);

                if (comp == 0)
                    return mid;

                if (comp < 0)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            return ~low;
        }
    }
}
