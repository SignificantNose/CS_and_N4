using Avalonia.Markup.Xaml.MarkupExtensions;
using Microsoft.CodeAnalysis.CSharp;
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

        protected override Task<QueryResult> AuthorizeClientAsync(string email, string password)
        {
            throw new NotImplementedException();
        }
        /*        // return null if auth is successful
                protected override async Task<string?> AuthorizeClientAsync(string email, string password)
                {
                    if (reader == null || writer == null) {
                        return "Implementation error: Streams are not set";
                    }

                    string? result = null;
                    string[] response;
                    string? responseLine;

                    responseLine = await reader.ReadLineAsync();
                    if (!(responseLine != null && responseLine.StartsWith("+OK")))
                    {
                        result = "Server connected, but refused service.";
                    }
                    else {
                        bool responseStatus;
                        await SmalltalkAsync($"USER {email}", false);
                        (response, responseStatus) = await SmalltalkAsync($"PASS {password}", false);
                        if (responseStatus)
                        {
                            // reformat for array
        *//*                    if (!(response != null && response.StartsWith("+OK")))
                            {
                                // then the string contains the amount of mail
                                // but do we need it though?
                                result = $"AUTH error: {response}";
                            }*//*
                        }
                    }
                }*/

        // if the return value is null, error occurred.
        public async Task<(string[], bool)> SmalltalkAsync(string query, bool multilineResponse) {
            if (reader == null || writer == null) {
                return (new string[]{ "READER/WRITER ARE NULL"},false);
            }

            await writer.WriteAsync(query);
            await writer.FlushAsync();

            List<string> res = new List<string>();
            string? responseLine;
            if (multilineResponse)
            {
                do
                {
                    responseLine = await reader.ReadLineAsync();
                    if (responseLine == null) {
                        res.Add("RECEIVED EMPTY RESPONSE");
                        return (res.ToArray(), false);
                    }
                    res.Add(responseLine);
                }
                while (responseLine != ".");
            }
            else { 
                responseLine = await reader.ReadLineAsync();
                if (responseLine == null) {
                    res.Add("RECEIVED EMPTY RESPONSE");
                    return (res.ToArray(), false);
                }
                res.Add(responseLine);
            }
            return (res.ToArray(), true);
        }
    }
}
