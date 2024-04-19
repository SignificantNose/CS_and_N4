using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Reactive;

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
        public Socket? ConnectionSocket { get; set; }


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
            ConnectionSocket = null;

            IObservable<bool> btnEnabled = this.WhenAnyValue(
                x => x.Email, x => x.Password, x => x.IsAuthAllowed,
                (x, y, z) => !string.IsNullOrEmpty(x) && !string.IsNullOrEmpty(y) && IsAuthAllowed
                );

            AuthenticateUser = ReactiveCommand.Create(
                () => {
                    // disable the login button 
                    IsAuthAllowed = false;
                    // try to connect 
                    ConnectionSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
                    // try to authenticate
                    ErrorText = "ERROR: NOT IMPLEMENTED";

                    // enable the buttons
                    IsAuthAllowed = true;
                },
                btnEnabled
            );
        }

        private void ClearError(object? sender, EventArgs e)
        {
            ErrorText = string.Empty;
        }
    }
}
