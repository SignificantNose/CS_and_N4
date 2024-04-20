using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS_and_N4.Models
{
    internal class POPCreator : ClientCreator
    {
        public override string Display => $"POP [s:{POPClient.encryptedPort} ns:{POPClient.nonEncryptedPort}]";

        public override POPClient CreateClient(bool encryptedChannel, string hostServer)
        {
            return new POPClient(encryptedChannel, hostServer);
        }
    }
}
