using CS_and_N4.Models;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reactive;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CS_and_N4.ViewModels
{
    public class MailBox
    {
        public string Name { get; set; }
        public string altName { get; set; }

        public MailBox(string Name, string altName)
        {
            this.Name = Name;
            this.altName = altName;
        }
    }
    public class DialogueStruct
    {
        public string Query { get; set; }
        public string Response { get; set; }
        public DialogueStruct(string Query, string[] Response)
        {
            this.Query = Query;
            StringBuilder sb = new StringBuilder();
            foreach (string response in Response)
            {
                sb.Append(response);
            }
            this.Response = sb.ToString();
        }
    }
    public class IMAPClientViewModel : ViewModelBase
    {

        public ReactiveCommand<Unit, Unit> QuitCommand { get; set; }
        protected IMAPClient client;

        [Reactive]
        public string CurrentContent { get; set; }

        public ObservableCollection<MailBox> MailBoxes { get; set; }
        public ObservableCollection<DialogueStruct> Log { get; set; }


        public IMAPClientViewModel(IMAPClient client)
        {
            this.client = client;
            MailBoxes = new ObservableCollection<MailBox>();
            Log = new ObservableCollection<DialogueStruct>();

            GetListOfMailboxesAsync();

            QuitCommand = ReactiveCommand.Create(() =>
            {
                // socket.close or something like that
                client.QuitSessionAsync();
            }
            );
        }



        public async Task GetListOfMailboxesAsync()
        {
            string[] data;
            string tag;

            string query = $"LIST \"\" *";
            (data, tag) = await client.SmalltalkAsync(query);

            if (tag == "")
            {
                // error occurred
            }
            else
            {
                // get the current response as a string
                List<MailBox> boxes = new List<MailBox>();

                // update the combobox
                foreach (string line in data)
                {
                    if (line.StartsWith("* LIST ")) {
                        // current line is a mail box

                        string pattern = @"\* LIST \(([^)]*)\) ""([^""]*)"" ""([^""]*)""";
                        Match match = Regex.Match(line, pattern);
                        if (match.Success) {
                            string altName = match.Groups[1].Value;
                            string Name = match.Groups[3].Value;
                            boxes.Add(new MailBox(Name, altName));
                        }

/*                        int startIdx, endIdx;
                        startIdx = line.IndexOf('(');
                        endIdx = line.IndexOf(')');
                        string altName = line.Substring(startIdx+1, endIdx-startIdx-1);*/

                    }
                }

                MailBoxes.Clear();
                MailBoxes.AddRange(boxes);
                // add to console log
                Log.Add(new DialogueStruct(tag + " " + query, data));
            }
        }

        public async Task SelectMailboxAsync(string boxName) {
            string[] data;
            string tag;

            string query = $"SELECT {boxName}";
            (data, tag) = await client.SmalltalkAsync(query);

            if (tag == "")
            {
                // error occurred
            }
            else {
                int counter = 0;
                foreach (string line in data) {
                    Debug.WriteLine($"{counter}: {line}");
                    counter++;
                }
            }
        }

        public async Task GetDateSubjectAsync(int startIdx, int? endIdx = null) {
            string tag;
            string[] data;
            
            string addPart = endIdx == null ? "" : $":{endIdx}";
            string query = $"FETCH {startIdx}{addPart} (BODY[HEADER.FIELDS (SUBJECT DATE)])";

            (data, tag) = await client.SmalltalkAsync(query);
            if (tag == "")
            {
                // error occurred
            }
            else { 
                // display data in a clickable listbox
                int counter = 0;
                foreach (string line in data) {
                    Debug.WriteLine($"{counter}: {line}");
                    counter++;
                }
            }
        }

        public async Task GetMailContentAsync(int msgIdx) {
            string tag;
            string[] data;

            string query = $"FETCH {msgIdx} (BODY[TEXT])";
            (data, tag) = await client.SmalltalkAsync(query);
            if (tag == "")
            {
                // error occurred
            }
            else
            {
                // display data in a clickable listbox
                int counter = 0;
                foreach (string line in data)
                {
                    Debug.WriteLine($"{counter}: {line}");
                    counter++;
                }
            }
        }
    }
}
