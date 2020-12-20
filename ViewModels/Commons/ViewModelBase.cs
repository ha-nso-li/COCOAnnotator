using COCOAnnotator.Services;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace COCOAnnotator.ViewModels.Commons {
    public abstract class ViewModelBase : BindableBase {
        private string _Title;
        public string Title {
            get => _Title;
            set => SetProperty(ref _Title, value);
        }

        protected IEventAggregator EventAggregator;
        protected IDialogService UserDialogSerivce;
        protected CommonDialogService CommonDialogService;
        protected SerializationService SerializationService;
        protected SettingService SettingService;

        protected ViewModelBase() {
            _Title = "";

            EventAggregator = ContainerLocator.Current.Resolve<IEventAggregator>();
            UserDialogSerivce = ContainerLocator.Current.Resolve<IDialogService>();
            CommonDialogService = ContainerLocator.Current.Resolve<CommonDialogService>();
            SerializationService = ContainerLocator.Current.Resolve<SerializationService>();
            SettingService = ContainerLocator.Current.Resolve<SettingService>();
        }
    }
}
