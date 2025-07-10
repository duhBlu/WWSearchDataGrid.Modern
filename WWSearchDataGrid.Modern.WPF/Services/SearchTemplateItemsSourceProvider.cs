using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.Core.Performance;

namespace WWSearchDataGrid.Modern.WPF.Services
{
    /// <summary>
    /// WPF-specific provider implementation for SearchTemplate's AvailableValues collection
    /// Handles dispatcher marshaling for thread-safe UI updates
    /// </summary>
    internal class SearchTemplateItemsSourceProvider : ISharedItemsSourceProvider
    {
        private readonly ObservableCollection<object> _collection;
        private readonly Dispatcher _dispatcher;

        public SearchTemplateItemsSourceProvider(ObservableCollection<object> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        public void UpdateItems(IEnumerable<object> items)
        {
            var itemsList = items?.ToList() ?? new List<object>();
            
            if (_dispatcher.CheckAccess())
            {
                UpdateItemsInternal(itemsList);
            }
            else
            {
                _dispatcher.BeginInvoke(new Action(() => UpdateItemsInternal(itemsList)));
            }
        }

        private void UpdateItemsInternal(IList<object> items)
        {
            // Preserve existing items and sync with new values without disrupting bindings
            SyncCollectionItems(items);
        }

        /// <summary>
        /// Synchronizes collection with new items while preserving existing items and their bindings
        /// </summary>
        private void SyncCollectionItems(IList<object> newItems)
        {
            var currentItems = _collection.ToList();

            // Remove items that are no longer in the new list
            for (int i = currentItems.Count - 1; i >= 0; i--)
            {
                if (!newItems.Contains(currentItems[i]))
                {
                    _collection.RemoveAt(i);
                }
            }

            // Add new items that aren't already present
            foreach (var newItem in newItems)
            {
                if (!_collection.Contains(newItem))
                {
                    // Insert in sorted order
                    var itemStr = newItem?.ToString() ?? string.Empty;
                    int insertIndex = 0;
                    
                    for (int i = 0; i < _collection.Count; i++)
                    {
                        var existingStr = _collection[i]?.ToString() ?? string.Empty;
                        if (string.Compare(itemStr, existingStr, StringComparison.Ordinal) < 0)
                        {
                            insertIndex = i;
                            break;
                        }
                        insertIndex = i + 1;
                    }
                    
                    _collection.Insert(insertIndex, newItem);
                }
            }
        }

        public void AddItem(object item)
        {
            if (_dispatcher.CheckAccess())
            {
                AddItemInternal(item);
            }
            else
            {
                _dispatcher.BeginInvoke(new Action(() => AddItemInternal(item)));
            }
        }

        private void AddItemInternal(object item)
        {
            if (!_collection.Contains(item))
            {
                // Insert in sorted order
                var itemStr = item?.ToString() ?? string.Empty;
                for (int i = 0; i < _collection.Count; i++)
                {
                    var existingStr = _collection[i]?.ToString() ?? string.Empty;
                    if (string.Compare(itemStr, existingStr, StringComparison.Ordinal) < 0)
                    {
                        _collection.Insert(i, item);
                        return;
                    }
                }
                _collection.Add(item);
            }
        }

        public void RemoveItem(object item)
        {
            if (_dispatcher.CheckAccess())
            {
                _collection.Remove(item);
            }
            else
            {
                _dispatcher.BeginInvoke(new Action(() => _collection.Remove(item)));
            }
        }
    }
}