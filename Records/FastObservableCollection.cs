using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace COCOAnnotator.Records {
    public class FastObservableCollection<T> : ObservableCollection<T> {
        public void AddRange(IEnumerable<T> collection) {
            if (Items is List<T> ListItems) {
                ListItems.AddRange(collection);
            } else {
                foreach (T i in collection) {
                    Items.Add(i);
                }
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        }

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

        public FastObservableCollection() : base() { }
        public FastObservableCollection(IEnumerable<T> collection) : base(collection) { }
        public FastObservableCollection(List<T> list) : base(list) { }
    }
}
