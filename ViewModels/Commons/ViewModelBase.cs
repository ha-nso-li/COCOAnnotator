using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace LabelAnnotator.ViewModels.Commons {
    public abstract class ViewModelBase : BindableBase {
        private string _Title;
        public string Title {
            get => _Title;
            set => SetProperty(ref _Title, value);
        }

        protected IEventAggregator EventAggregator;
        protected IDialogService UserDialogSerivce;
        protected Services.CommonDialogService CommonDialogService;
        protected Services.SerializationService SerializationService;
        protected Services.SettingService SettingService;

        protected ViewModelBase() {
            _Title = "";

            EventAggregator = ContainerLocator.Current.Resolve<IEventAggregator>();
            UserDialogSerivce = ContainerLocator.Current.Resolve<IDialogService>();
            CommonDialogService = ContainerLocator.Current.Resolve<Services.CommonDialogService>();
            SerializationService = ContainerLocator.Current.Resolve<Services.SerializationService>();
            SettingService = ContainerLocator.Current.Resolve<Services.SettingService>();
        }
    }
}
