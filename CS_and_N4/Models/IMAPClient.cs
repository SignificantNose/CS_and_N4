using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS_and_N4.Models
{
    public class IMAPClient : ClientBase
    {
        public static readonly int encryptedPort = 993;
        public override int EncryptedPort => encryptedPort;

        public static readonly int nonEncryptedPort = 143;
        public override int NonEncryptedPort => nonEncryptedPort;

        protected int tagCounter;

        public IMAPClient(bool encryptedChannel, string hostServer) : base(encryptedChannel, hostServer)
        {
            tagCounter = 0;
        }

        // basically a combination of SendMessage and ReceiveResponse
        // (to avoid making 2 method calls)      
        // important note: the last line will have a status. the validity must
        // be checked by the status line!!
        protected async Task<(string[], string)> SmalltalkAsync(string query) {
            if (reader == null || writer == null) return ([], "");
            string tag = $"A{tagCounter}";
            await writer.WriteAsync($"{tag} {query}\r\n");
            await writer.FlushAsync();
            tagCounter++;

            List<string> responseByLines = new List<string>();
            string? response = null;
            do {
                response = await reader.ReadLineAsync();
                responseByLines.Add(response);
            }
            while(response!=null && !response.StartsWith(tag));
            return (responseByLines.ToArray(), tag);
        }

        // returns null if authentication is successful
        protected override async Task<string?> AuthorizeClientAsync(string email, string password)
        {
            if (reader == null || writer == null) {
                return "Implementation error: Streams are not set";
            }

            string? result = null;

            string? response;
            // expected: * OK ...
            response = await reader.ReadLineAsync();
            if (!(response != null && response.StartsWith("* OK ")))
            {
                result = "Server connected, but refused service.";
            }
            else {
                // all good. authenticate

                // A1 LOGIN EMAIL PASSWORD
                // return value must not be null: checked stream for null before.
                string[] authResult;
                string tag;
                (authResult, tag)= await SmalltalkAsync ($"LOGIN {email} {password}");

                // expected: * CAPABILITIES ...
                //           A1 OK ...
                if (!(tag!="" && authResult[authResult.Length - 1].StartsWith($"{tag} OK")))
                {
                    result = $"AUTH error: {authResult[authResult.Length - 1]}";
                }
            }

            return result;
        }

        public async Task QuitSessionAsync() {
            await SmalltalkAsync("CLOSE");
            await SmalltalkAsync("LOGOUT");
            tagCounter = 0;
            CloseConnection();
        }




        protected async Task<string?> SendMessageAsync(string msg)
        {
            if (writer == null) return null;

            string tag = $"A{tagCounter}";
            await writer.WriteAsync($"{tag} {msg}\r\n");
            await writer.FlushAsync();
            tagCounter++;
            return tag;
        }

        protected async Task<string[]> ReceiveResponseAsync(string tag)
        {
            if (reader == null) return [];

            List<string> responseByLines = new List<string>();
            string? response = null;
            do
            {
                response = await reader.ReadLineAsync();
                responseByLines.Add(response);
            }
            while (response != null && !response.StartsWith(tag));
            return responseByLines.ToArray();
        }
    }
}
