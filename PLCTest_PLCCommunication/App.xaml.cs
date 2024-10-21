using System.Windows;
using CvsService.PLC.Mitsubishi.Services;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Mvvm;

namespace PLCTest_PLCCommunication_v2
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IMitsubishiPLC, MitsubishiPLC> ();
            ViewModelLocationProvider.Register<MainWindow, MainViewModel>();
        }
    }
}
