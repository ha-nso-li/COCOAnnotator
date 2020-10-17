using CommonServiceLocator;
using Prism.Events;
using Prism.Mvvm;

namespace LabelAnnotator.ViewModels.Commons {
    public abstract class ViewModelBase : BindableBase {
        protected IEventAggregator EventAggregator;

        protected ViewModelBase() {
            EventAggregator = ServiceLocator.Current.GetInstance<IEventAggregator>();
        }
    }
}
