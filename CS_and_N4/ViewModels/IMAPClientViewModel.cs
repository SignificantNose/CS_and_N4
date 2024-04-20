using CS_and_N4.Models;
using ReactiveUI;
using System.Net.Sockets;
using System.Reactive;

namespace CS_and_N4.ViewModels
{
    public class IMAPClientViewModel : ViewModelBase
    {

        public ReactiveCommand<Unit, Unit> QuitCommand { get; set; }

        public IMAPClientViewModel(IMAPClient client) {
            QuitCommand = ReactiveCommand.Create(() => 
            {
                // socket.close or something like that
                client.QuitSessionAsync();
            }
            );
        }
    }
}
