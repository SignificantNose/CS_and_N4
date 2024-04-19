using ReactiveUI;

namespace CS_and_N4.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        /*        private ViewModelBase _currentViewModel;
                public ViewModelBase CurrentViewModel {
                    get => _currentViewModel;
                    set => this.RaiseAndSetIfChanged(ref _currentViewModel, value); 
                }*/

        public ViewModelBase CurrentViewModel { get; set; }
        public MainWindowViewModel() {
            CurrentViewModel = new AuthorizationViewModel();
        }
    }
}
