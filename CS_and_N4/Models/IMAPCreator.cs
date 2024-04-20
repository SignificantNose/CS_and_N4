using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS_and_N4.Models
{
    internal class IMAPCreator : ClientCreator
    {
        public override string Display => $"IMAP [s:{IMAPClient.encryptedPort} ns:{IMAPClient.nonEncryptedPort}]";

        public override IMAPClient CreateClient(bool encryptedChannel, string hostServer)
        {
            return new IMAPClient(encryptedChannel, hostServer);
        }
    }
}
