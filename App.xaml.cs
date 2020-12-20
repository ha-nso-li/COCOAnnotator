using COCOAnnotator.Services;
using COCOAnnotator.Views;
using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;

namespace COCOAnnotator {
    public partial class App : PrismApplication {
        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            containerRegistry.RegisterSingleton<CommonDialogService>();
            containerRegistry.RegisterSingleton<SettingService>();
            containerRegistry.RegisterSingleton<SerializationService>();

            containerRegistry.RegisterDialog<SettingDialog>();
            containerRegistry.RegisterDialog<ManageDialog>();
        }
    }
}
