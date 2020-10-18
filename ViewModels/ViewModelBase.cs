using CommonServiceLocator;
using Prism.Events;
using Prism.Mvvm;

namespace LabelAnnotator.ViewModels {
    public abstract class ViewModelBase : BindableBase {
        protected IEventAggregator EventAggregator;
        protected Services.DialogService DialogService;

        protected ViewModelBase() {
            EventAggregator = ServiceLocator.Current.GetInstance<IEventAggregator>();
            DialogService = ServiceLocator.Current.GetInstance<Services.DialogService>();
        }
    }
}
