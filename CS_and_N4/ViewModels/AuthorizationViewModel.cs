using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using System;
using System.Diagnostics;
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
        private string _email;
        private string _password;
        private bool _isAuthAllowed;
        private bool _useEncryption;
        private string _errorText;

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


            IObservable<bool> btnEnabled = this.WhenAnyValue(
                x => x.Email, x => x.Password, x => x.IsAuthAllowed,
                (x, y, z) => !string.IsNullOrEmpty(x) && !string.IsNullOrEmpty(y) && IsAuthAllowed
                );

            AuthenticateUser = ReactiveCommand.Create(
                () => {
                    // disable the login button 
                    IsAuthAllowed = false;
                    // try to connect 
                    // try to authenticate
                    // switch the view
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

        public string Email { 
            get=> _email; 
            set => this.RaiseAndSetIfChanged(ref _email, value); 
        }

        public string Password { 
            get => _password; 
            set => this.RaiseAndSetIfChanged(ref _password, value); 
        }
        public bool IsAuthAllowed
        {
            get => _isAuthAllowed;
            set => this.RaiseAndSetIfChanged(ref _isAuthAllowed, value);
        }
        public bool UseEncryption
        {
            get => _useEncryption;
            set => this.RaiseAndSetIfChanged(ref _useEncryption, value);
        }
        public string ErrorText {
            get => _errorText;
            set {
                _errorClearTimer.Stop();
                if (!string.IsNullOrEmpty(value)) {
                    _errorClearTimer.Start();
                }
                this.RaiseAndSetIfChanged(ref _errorText, value);
            }
        }
    }
}
