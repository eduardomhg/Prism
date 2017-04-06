using System;
using Microsoft.Practices.Unity;
using Prism.Unity;
using PrismUnityApp1.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Prism.Regions;
using PrismUnityApp1.ViewModels;

namespace PrismUnityApp1
{
    class Bootstrapper : UnityBootstrapper
    {
        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();

            Container.RegisterType<object, View1>("View1");
            Container.RegisterType<object, View2>("View2");
        }

        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
            ThreadLogger.Log($"Dispatcher thread is {Dispatcher.CurrentDispatcher.Thread.ManagedThreadId}", GetType().Name);

            var regionManager = this.Container.Resolve<IRegionManager>();

            ThreadLogger.Log("Before navigating", GetType().Name);

            regionManager.RequestNavigate("ContentRegion1", "View1");

            ThreadLogger.Log("After navigating to view 1", GetType().Name);

            regionManager.RequestNavigate("ContentRegion2", "View2");

            ThreadLogger.Log("After navigating to view 2", GetType().Name);

            Application.Current.MainWindow.Show();
        }
    }
}
