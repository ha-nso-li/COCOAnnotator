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

        protected override async void RegisterTypes(IContainerRegistry containerRegistry) {
            containerRegistry.RegisterInstance(await SettingService.Read().ConfigureAwait(false));
            containerRegistry.RegisterDialog<SettingDialog>();
            containerRegistry.RegisterDialog<ManageDialog>();
        }
    }
}
