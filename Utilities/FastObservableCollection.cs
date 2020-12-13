using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LabelAnnotator.Utilities {
    public class FastObservableCollection<T> : ObservableCollection<T> {
        public void RemoveAll(Predicate<T> match) {
            ((List<T>)Items).RemoveAll(match);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        }
    }
}
