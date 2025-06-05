using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    internal static class CollectionExtensions
    {
        internal static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable.ToList())
            {
                if (item != null)
                    action(item);
            }
        }
    }
}
