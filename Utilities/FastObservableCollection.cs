using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace COCOAnnotator.Utilities {
    public class FastObservableCollection<T> : ObservableCollection<T> {
        public int RemoveAll(Predicate<T> match) {
            int removedCount = ((List<T>)Items).RemoveAll(match);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

            return removedCount;
        }
    }
}
