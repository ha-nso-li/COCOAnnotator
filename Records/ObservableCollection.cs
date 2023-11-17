using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace COCOAnnotator.Records {
    public class ObservableCollection<T> : System.Collections.ObjectModel.ObservableCollection<T> {
        public void AddRange(IEnumerable<T> collection) {
            CheckReentrancy();

            List<T> add = collection.ToList();
            List<T> items = (List<T>)Items;
            items.AddRange(add);

            if (add.Count > 0) {
                OnCollectionChanged(new(NotifyCollectionChangedAction.Add, add));
                OnPropertyChanged(new(nameof(Count)));
                OnPropertyChanged(new("Item[]"));
            }
        }

        public int RemoveAll(Predicate<T> match) {
            CheckReentrancy();

            List<T> removed = [];
            List<T> items = (List<T>)Items;
            foreach (T i in items) {
                if (match(i)) removed.Add(i);
            }
            items.RemoveAll(match);

            if (removed.Count > 0) {
                OnCollectionChanged(new(NotifyCollectionChangedAction.Remove, removed));
                OnPropertyChanged(new(nameof(Count)));
                OnPropertyChanged(new("Item[]"));
            }

            return removed.Count;
        }

        public ObservableCollection() : base() { }
        public ObservableCollection(IEnumerable<T> collection) : base(collection) { }
        public ObservableCollection(List<T> list) : base(list) { }
    }
}
