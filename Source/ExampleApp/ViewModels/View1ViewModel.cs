using System.Diagnostics;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Prism.Mvvm;
using Prism.Regions;

namespace PrismUnityApp1.ViewModels
{
    public class View1ViewModel : BindableBase, INavigationAware
    {
        private readonly IRegionManager _regionManager;

        public View1ViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            //Debug.WriteLine($"{nameof(View1ViewModel)} created");
        }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            //Debug.WriteLine($"{nameof(View1ViewModel)} OnNavigatedTo");

            ThreadLogger.Log("Before loading View 1 data", GetType().Name);
            Text = "Loading View 1...";
            await DoSomethingAsync();
            Text = "View 1 Loaded Complete!";
            ThreadLogger.Log("After loading View 1 data", GetType().Name);
        }

        private Task DoSomethingAsync()
        {
            return Task.Run(() =>
            {
                Thread.Sleep(5000);
            });
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        private string text;
        public string Text { get { return text; } private set { text = value; RaisePropertyChanged(); } }
    }
}
