using Avalonia.Threading;
using Avalonia.Utilities;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reactive.Linq;

namespace CS_and_N4.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        /*        private ViewModelBase _currentViewModel;
                public ViewModelBase CurrentViewModel {
                    get => _currentViewModel;
                    set => this.RaiseAndSetIfChanged(ref _currentViewModel, value); 
                }*/


        // A decision has been made to not use Routing as a navigation tool, as
        // a return parameter must be present in switching the views.
        // Even though the Routing approach might seem as a more convenient one.

        [Reactive]
        public ViewModelBase CurrentViewModel { get; set; }
        public AuthorizationViewModel Authorization { get; }
        private bool SessionActive = false;
        public MainWindowViewModel() {
            Authorization = new AuthorizationViewModel();

            // subscribe to an observable of the Authorization
            // basically need to switch up the view
            Authorization
                .WhenAnyValue(vm => vm.ConnectionSocket)
                .Subscribe((Socket? sock )=> 
                {
                    if (sock != null && !SessionActive) { 
                        StartIMAPSession(sock);
                    }
                }
                );
                
            CurrentViewModel = Authorization;
        }
        private void StartIMAPSession(Socket sock) {
            SessionActive = true;

            IMAPClientViewModel IMAPClient = new IMAPClientViewModel(sock);
            // change SessionActive based on the IMAPClientViewModel changes
            IMAPClient.QuitCommand.Subscribe(
                (_) => 
                { 
                    CurrentViewModel = Authorization;
                    SessionActive = false;
                }
                );
            CurrentViewModel = IMAPClient;
        }
    }
}
