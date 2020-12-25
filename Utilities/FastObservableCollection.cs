using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace COCOAnnotator.Utilities {
    public class FastObservableCollection<T> : ObservableCollection<T> {
        public int RemoveAll(Predicate<T> match) {
            int removedCount;
            if (Items is List<T> ListItems) {
                removedCount = ListItems.RemoveAll(match);
            } else {
                removedCount = 0;
                for (int i = 0; i < Items.Count; i++) {
                    if (match(Items[i])) {
                        Items.RemoveAt(i);
                        i--;
                        removedCount++;
                    }
                }
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

            return removedCount;
        }
    }
}
