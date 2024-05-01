using Avalonia.Threading;
using Avalonia.Utilities;
using CS_and_N4.Models;
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
                .WhenAnyValue(vm => vm.ConnectedClient)
                .Subscribe((ClientBase? ConnectedClient )=> 
                {
                    if (ConnectedClient != null) {
                        if (!SessionActive)
                        {
                            StartIMAPSession((IMAPClient)ConnectedClient);
                        }
                        else {
                            ConnectedClient.CloseConnection();
                        }
                    }
                }
                );
                
            CurrentViewModel = Authorization;
        }
        private void StartIMAPSession(IMAPClient client) {
            SessionActive = true;

            IMAPClientViewModel IMAPClient = new IMAPClientViewModel(client);
            // change SessionActive based on the IMAPClientViewModel changes
            IMAPClient.QuitCommand.Subscribe(
                (errorMsg) => 
                { 
                    CurrentViewModel = Authorization;
                    Authorization.ErrorText = errorMsg == null ? "Session ended." : errorMsg;
                    SessionActive = false;
                }
                );
            CurrentViewModel = IMAPClient;
        }
    }
}
