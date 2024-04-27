using Avalonia.Threading;
using CS_and_N4.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reactive;
using System.Threading.Tasks;

namespace CS_and_N4.ViewModels
{
    public class AuthorizationViewModel : ViewModelBase
    {

        // the property is binded, but if I change it in the code explicitly, 
        // the value will not change in the UI. In order to make it work, a
        // setter with this.RaiseAndSetIfChanged must be instantiated.
        // imo CommunityToolkit is better with its [ObservableProperty] attribute
        // upd: I need to have the properties for these values: Authenticate button
        // must be enabled when these values are not empty.
        // this is easily modifiable to [ObservableProperty] just in case
        //
        // upd: a similar approach in ReactiveUI is to use fody to 
        // make a [Reactive] attribute to avoid boilerplate code

        [Reactive]
        public string Email { get; set; }
        [Reactive]
        public string Password { get; set; }
        [Reactive]
        public bool IsAuthAllowed { get; set; }
        [Reactive]
        public bool UseEncryption { get; set; }

        [Reactive]
        public ClientBase? ConnectedClient { get; set; }

        public ObservableCollection<ClientCreator> Protocols { get; } = new ObservableCollection<ClientCreator>()
        {
            new IMAPCreator(),
            new POPCreator()
        };

        [Reactive]
        public int SelectedProtocolIdx { get; set; }

        [Reactive]
        public string HostServerAddress { get; set; }


        private string _errorText;
        public string ErrorText
        {
            get => _errorText;
            set
            {
                _errorClearTimer.Stop();
                if (!string.IsNullOrEmpty(value))
                {
                    _errorClearTimer.Start();
                }
                this.RaiseAndSetIfChanged(ref _errorText, value);
            }
        }

        public ReactiveCommand<Unit, Unit> AuthenticateUser { get; }

        private DispatcherTimer _errorClearTimer;
        private static readonly int timerIntervalMS = 5000;

        public AuthorizationViewModel() {
            _errorClearTimer = new DispatcherTimer();
            _errorClearTimer.Tick += ClearError;
            _errorClearTimer.Interval = TimeSpan.FromMilliseconds(timerIntervalMS);

            Email = string.Empty;
            Password = string.Empty;
            IsAuthAllowed = true;
            UseEncryption = true;
            ErrorText = string.Empty;
            ConnectedClient = null;
            SelectedProtocolIdx = 0;
            HostServerAddress = string.Empty;

            IObservable<bool> btnEnabled = this.WhenAnyValue(
                x => x.Email, x => x.Password, x => x.HostServerAddress, x => x.IsAuthAllowed,
                (email, pass, host, allowed) =>
                    !string.IsNullOrEmpty(email) &&
                    !string.IsNullOrEmpty(pass) &&
                    !string.IsNullOrEmpty(host) &&
                    IsAuthAllowed
                );

            AuthenticateUser = ReactiveCommand.CreateFromTask(
                ConnectToServerAsync,
                btnEnabled
            );

        }

        private void ClearError(object? sender, EventArgs e)
        {
            ErrorText = string.Empty;
        }

        private async Task ConnectToServerAsync() {
            IsAuthAllowed = false;
            ClientBase client = Protocols[SelectedProtocolIdx].CreateClient(UseEncryption, HostServerAddress);
            string? result = await client.AuthenticateAsync(Email, Password);
            if (result != null)
            {
                ErrorText = result.ToString();
            }
            else
            {
                ConnectedClient = client;
            }

            IsAuthAllowed = true;
        }
    }
}
