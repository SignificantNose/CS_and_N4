using Avalonia.Markup.Xaml.MarkupExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS_and_N4.Models
{
    internal class POPClient : ClientBase
    {
        public static readonly int encryptedPort = 995;
        public override int EncryptedPort => encryptedPort;

        public static readonly int nonEncryptedPort = 110;
        public override int NonEncryptedPort => nonEncryptedPort;

        public POPClient(bool encryptedChannel, string hostServer): base(encryptedChannel, hostServer) 
        { }

        protected override Task<string?> AuthorizeClientAsync(string email, string password)
        {
            throw new NotImplementedException();
        }
    }
}
