﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
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

        /// <summary>
        /// Queries the server based on the provided query
        /// </summary>
        /// <param name="query"></param>
        /// <returns>
        /// QueryResponse.status = true -> data contains all the received data. Tag contains the tag used to query the server <para />
        /// QueryResponse.status = false -> data is null. Tag contains the error message.
        /// </returns>
        public async Task<QueryResult> SmalltalkAsync(string query) {
            if (reader == null || writer == null) return new QueryResult(false, "READER/WRITER IS NULL", null);

            string tag = $"A{tagCounter}";
            List<string> responseByLines = new List<string>();
            bool status = true;

            try { 
                await writer.WriteAsync($"{tag} {query}\r\n");
                await writer.FlushAsync();
                tagCounter++;

                string? response = null;
                do {
                    response = await reader.ReadLineAsync();
                    if (response == null) {
                        status = false;
                        tag = "Error: null string received";
                        break;
                    }
                    responseByLines.Add(response);
                }
                while(response!=null && !response.StartsWith(tag));
            }
            catch (Exception ex)
            {
                return new QueryResult(false, $"Exception occurred: {ex.Message}", null);
            }
            return new QueryResult(status, tag, responseByLines.ToArray());
        }

        protected override async Task<QueryResult> AuthorizeClientAsync(string email, string password)
        {
            if (reader == null || writer == null) {
                return new QueryResult(false, "Implementation error: Streams are not set", null);
            }

            string result = "";
            bool status = true;
            string? response;

            // expected: * OK ...
            response = await reader.ReadLineAsync();
            if (!(response != null && response.StartsWith("* OK ")))
            {
                result = "Server connected, but refused service.";
                status = false;
            }
            else {
                // all good. authenticate

                // A1 LOGIN EMAIL PASSWORD
                // return value must not be null: checked stream for null before.
                QueryResult qResponse = await SmalltalkAsync($"LOGIN {email} {password}");

                // expected: * CAPABILITIES ...
                //           A1 OK ...

                string[] data = qResponse.data;
                if (qResponse.status)
                {
                    if (!(data[data.Length - 1].StartsWith($"{qResponse.header} OK"))) {
                        status = false;
                        result = $"AUTH error: {data[data.Length - 1]}";
                    }
                }
                else {
                    result = $"Error: {qResponse.header}";
                    status = false;
                }
            }
            return new QueryResult(status, result, null);
        }



        /// <summary>
        /// Initiates end of session. Even if the error has occurred, it will still try to end the connection.
        /// </summary>
        /// <param name="exitMsg">Error message that had caused the caller to close the connection.</param>
        /// <returns>Either error message provided by the caller, 
        /// or (if the provided parameter is null) the result of closing the connection</returns>
        public async Task<string?> InitiateQuitAsync(string? exitMsg = null) {
            QueryResult qResult;
            string? result = exitMsg;

            // CLOSE command is not very relevant in this situation.
            //string[] queries = ["CLOSE", "LOGOUT"];
 
            qResult = await SmalltalkAsync("LOGOUT");
            if (result == null)
            {
                if (!qResult.status)
                {
                    result = qResult.header;
                }

                else {
                    // checking if BYE response is valid.
                    // assuming that the response contains only 2 strings: BYE string and OK string
                    if (!(qResult.data.Length == 2 && qResult.data[0].Contains("BYE") && qResult.data[1].Contains("OK"))) {
                        result = "Exit error: invalid LOGOUT response";
                    }
                }
            }
            tagCounter = 0;
            CloseConnection();
            return result;
        }

        // this is basically the same as InitiateQuitAsync, 
        // it just doesn't return any message.
        public async Task QuitSessionAsync() {
            await SmalltalkAsync("CLOSE");
            await SmalltalkAsync("LOGOUT");
            tagCounter = 0;
            CloseConnection();
        }


        public async Task<QueryResult> SendMessageAsync(string msg)
        {
            if (writer == null) return new QueryResult(false, "Writer is null", null);

            string tag = $"A{tagCounter}";
            await writer.WriteAsync($"{tag} {msg}\r\n");
            await writer.FlushAsync();
            tagCounter++;
            return new QueryResult(true, tag, null);
        }

        public async Task<QueryResult> ReceiveResponseAsync(string tag)
        {
            if (reader == null) return new QueryResult(false, "Reader is null", null);

            List<string> responseByLines = new List<string>();
            string? response;
            bool status = true;
            do
            {
                response = await reader.ReadLineAsync();
                if (response == null)
                {
                    status = false;
                    tag = "Error: null string received";
                    break;
                }
                responseByLines.Add(response);
            }
            while (response != null && !response.StartsWith(tag));
            return new QueryResult(status, tag, responseByLines.ToArray());
        }
    }
}
