using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace COCOAnnotator.Records {
    public class ObservableCollection<T> : System.Collections.ObjectModel.ObservableCollection<T> {
        public void AddRange(IEnumerable<T> collection) {
            if (Items is List<T> ListItems) {
                ListItems.AddRange(collection);
            } else {
                foreach (T i in collection) {
                    Items.Add(i);
                }
            }

            OnCollectionChanged(new(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(new(nameof(Count)));
            OnPropertyChanged(new("Item[]"));
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

            OnCollectionChanged(new(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(new(nameof(Count)));
            OnPropertyChanged(new("Item[]"));

            return removedCount;
        }

        public ObservableCollection() : base() { }
        public ObservableCollection(IEnumerable<T> collection) : base(collection) { }
        public ObservableCollection(List<T> list) : base(list) { }
    }
}
