using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS_and_N4.Models
{
    public abstract class ClientCreator
    {
        public abstract string Display { get; }
        public abstract ClientBase CreateClient(bool encryptedChannel, string hostServer);
    }
}
