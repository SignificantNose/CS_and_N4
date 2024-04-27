using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace CS_and_N4.Models
{
    public class QueryResult {
        public bool status;
        public string header;
        public string[]? data;

        public QueryResult(bool status, string header, string[]? data) {
            this.status = status;
            this.header = header;
            this.data = data;
        }
    }

    public abstract class ClientBase
    {
        public abstract int EncryptedPort { get; }
        public abstract int NonEncryptedPort { get; }

        protected TcpClient? client = null;
        protected StreamReader? reader = null;
        protected StreamWriter? writer = null;

        public bool encChannel;
        public string host;

        public ClientBase(bool encryptedChannel, string hostServer) {
            encChannel = encryptedChannel;
            host = hostServer;
        }

        // method used as a dialogue between a client and 
        // a server. the connection is established, it's
        // only necessary to authenticate as a valid user
        protected abstract Task<QueryResult> AuthorizeClientAsync(string email, string password);


        public async Task<string?> AuthenticateAsync(string email, string password) {
            string? result = null;
            client = new TcpClient();

            Stream? preStream = null;
            try
            {
                await client.ConnectAsync(host, encChannel ? EncryptedPort : NonEncryptedPort);

                preStream = client.GetStream();
                if (encChannel)
                {
                    SslStream tempStream = new SslStream(preStream);
                    await tempStream.AuthenticateAsClientAsync(host);
                    preStream = tempStream;
                }

                reader = new StreamReader(preStream);
                writer = new StreamWriter(preStream);

                QueryResult qResponse = await AuthorizeClientAsync(email, password);
                if (!qResponse.status) {
                    result = qResponse.header;
                }
            }
            catch (AuthenticationException authEx)
            {
                result = $"Security channel authentication failed: {authEx.Message}";
            }
            catch (SocketException socketEx)
            {
                result = $"Error while trying to connect to server: {socketEx.Message}";
            }
            catch (Exception ex) 
            {
                result = $"Some error occurred: {ex.Message}";
            }



            if (result != null)
            {
                reader?.Close();
                writer?.Close();
                preStream?.Close();
                client?.Close();

                client = null;
                reader = null;
                writer = null;
            }

            return result;
        }

        public void CloseConnection() {
            reader?.Close();
            writer?.Close();
            client?.Close();
            client = null;
            reader = null;
            writer = null;
        }
    }
}
