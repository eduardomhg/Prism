using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Prism.Mvvm;
using Prism.Regions;

namespace PrismUnityApp1.ViewModels
{
    public class View2ViewModel : BindableBase, INavigationAware
    {
        public View2ViewModel()
        {
            //Debug.WriteLine($"{nameof(View2ViewModel)} created");
        }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
           // Debug.WriteLine($"{nameof(View2ViewModel)} OnNavigatedTo");

            ThreadLogger.Log("Before loading View 2 data", GetType().Name);
            Text = "Loading View 2...";
            await DoSomethingAsync();
            Text = "View 2 Loaded Complete!";
            ThreadLogger.Log("After loading View 2 data", GetType().Name);
        }

        private Task DoSomethingAsync()
        {
            return Task.Run(() =>
            {
                Thread.Sleep(2000);
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