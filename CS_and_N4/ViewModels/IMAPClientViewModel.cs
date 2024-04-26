using CS_and_N4.Models;
using DynamicData;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CS_and_N4.ViewModels
{
    public class IMAPClientViewModel : MailClientViewModelBase
    {

        public ReactiveCommand<Unit, Unit> QuitCommand { get; set; }
        protected IMAPClient client;

        
        private int _selectedMailboxIdx;
        public int SelectedMailboxIdx
        {
            get => _selectedMailboxIdx;
            set
            {
                // set the new contents of the mailbox mail list
                SelectMailboxAsync(MailBoxes[value]);

                this.RaiseAndSetIfChanged(ref _selectedMailboxIdx, value);
            }
        }

        [Reactive]
        public string CurrentContent { get; set; }
        public ObservableCollection<MailMessage> CurrentMailList { get; set; } = new ObservableCollection<MailMessage>();

        public ObservableCollection<MailBox> MailBoxes { get; set; } = new ObservableCollection<MailBox>();

        public IMAPClientViewModel(IMAPClient client)
        {
            this.client = client;

            // acquiring the list of mailbox and making the 
            // initially selected mailbox an empty one
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
                MailBoxes.Add(new MailBox(MAILBOX_ALT_EMPTY, MAILBOX_EMPTY));
                MailBoxes.AddRange(boxes);
                SelectedMailboxIdx = 0;
                // add to console log
                //Log.Add(new DialogueStruct(tag + " " + query, data));
            }
        }

        protected async Task SelectMailboxAsync(MailBox mb) {
            if (mb.Name == MAILBOX_EMPTY && mb.altName == MAILBOX_ALT_EMPTY) return;


            string[] selectResponse = await QuerySelectMailboxAsync(mb.Name);
            if (selectResponse.Length > 0) {
                int amntOfMail = 0;
                foreach (string response in selectResponse)
                {
                    if (response.Contains("EXISTS")){
                        // assuming that the string contains data of type: 
                        // "* NNN EXISTS"
                        string pattern = @"\* (\d+) EXISTS";
                        Match match = Regex.Match(response, pattern);
                        if (match.Success)
                        {
                            amntOfMail = int.Parse(match.Groups[1].Value);
                        }
                        else { 
                            // error: string of other type
                        }
                        break;
                    }
                }
                if (!(mb.MailCount != null && mb.MailCount == amntOfMail)) {
                    mb.MailCount = amntOfMail;
                    // now the max amount of pages is set
                }


                // display page; it'll deal with the unknown mail
                DisplayMailPageAsync(mb, 0);
            }
            else { 
                // error in response
            }
            // find the IDs if they are not found

                // change the count of messages


/*                int[] indices = await QuerySearchAllAsync();
                foreach(int i in indices)
                {
                    mb.Mail.Add(i, null);
                }*/
            

        }

        protected async Task DisplayMailPageAsync(MailBox mb, int index) {
            CurrentMailList.Clear();


            if (mb.MailCount != null)
            {
                int maxPageIdx = mb.MaxPages - 1;
                if (index > maxPageIdx) index = maxPageIdx;
                if (index < 0) index = 0;

                int startIdx = index * MailBox.mailPerPage+1;
                int endIdx = (index + 1) * MailBox.mailPerPage;

                if (endIdx > mb.MailCount) endIdx = mb.MailCount.Value;


                // in order to not add these items to a CurrentMailList and 
                // not make it re-appear every time
                List<MailMessage> currMsgRange = new List<MailMessage>();

                for (int i = startIdx; i <= endIdx; i++)
                {
                    // check if the current email is present
                    if (!mb.Mail.ContainsKey(i))
                    {
                        // query the server for this range of messages
                        // upd: what if there are only a few messages that need to be queried?
                        // upd: imo it's better to ask for a range than to ask for each message separately


                        // ask for headers 
                        // then, update the 
                        MailMessage[] mail = await QueryMsgHeaderAsync(startIdx, endIdx);
                        if (mail.Length != endIdx - startIdx+1)
                        {
                            // error: some messages are not parsed
                        }
                        else {
                            for (int j = startIdx; j <= endIdx; j++) {
                                if (mb.Mail.ContainsKey(j))
                                {
                                    mb.Mail[j] = mail[j - startIdx];
                                }
                                else { 
                                    mb.Mail.Add(j, mail[j-startIdx]);
                                }
                            }
                        }

                        break;
                    }
                }

                for (int i = startIdx; i <= endIdx; i++) { 
                    currMsgRange.Add(mb.Mail[i]);
                }

                CurrentMailList.AddRange(currMsgRange);
            }
            else { 
                // unexpected error
            }
        }

        protected async Task<MailMessage[]> QueryMsgHeaderAsync(int startIdx, int endIdx) {
            string[] data;
            string tag;

            string query = $"FETCH {startIdx}:{endIdx} (BODY[HEADER.FIELDS (SUBJECT DATE)])";
            (data, tag) = await client.SmalltalkAsync(query);

            if (tag == "")
            {
                // error occurred
                return [];
            }
            else
            {
                int counter = 0;
                foreach (string line in data)
                {
                    Debug.WriteLine($"{counter}: {line}");
                    counter++;
                }

                List<MailMessage> mailMsgs = new List<MailMessage>();
                string date = "";
                string subject = "";
                for (int i = 1; i < data.Length-1; i++) {
                    string currString = data[i];
                    if (currString == ")")
                    {
                        mailMsgs.Add(new MailMessage(subject, date));

                        date = "";
                        subject = "";
                    }
                    else {
                        if (currString.StartsWith("Date: "))
                        {
                            date = currString.Substring(5);
                        }
                        else if (currString.StartsWith("Subject: ")) {
                            subject = currString.Substring(8);
                        }
                    }
                }

                // take the date string, remove it from the string poll
                // the subject will be anything else

                return mailMsgs.ToArray();
            }
        }

        protected async Task<int[]> QuerySearchAllAsync()
        {
            string[] data;
            string tag;

            string query = "SEARCH ALL";
            (data, tag) = await client.SmalltalkAsync(query);

            if (tag == "")
            {
                // error occurred
                return [];
            }
            else
            {
                // get the substring
                if (data.Length != 2) {
                    // not implemented: the mailbox is too large
                    // wrong approach. must fix.
                    throw new NotImplementedException();
                }
                string indicesResponse = data[0];
                string keyword = "SEARCH ";
                int idxStart = indicesResponse.IndexOf(keyword) + keyword.Length;
                string indices = indicesResponse.Substring(idxStart);

                // split it and convert it
                string[] indicesStrings = indices.Split(' ');
                int[] indicesValues = Array.ConvertAll(indicesStrings, int.Parse);
                return indicesValues;
            }
        }

        public async Task<string[]> QuerySelectMailboxAsync(string boxName) {
            string[] data;
            string tag;

            string query = $"SELECT {boxName}";
            (data, tag) = await client.SmalltalkAsync(query);

            if (tag == "")
            {
                // error occurred
                return [];
            }
            else {
                int counter = 0;
                foreach (string line in data) {
                    Debug.WriteLine($"{counter}: {line}");
                    counter++;
                }
                return data;
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
