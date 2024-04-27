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
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CS_and_N4.ViewModels
{
    public class IMAPClientViewModel : MailClientViewModelBase
    {
        [Reactive]
        public bool GlobalEnabler { get; set; }


        public ReactiveCommand<Unit, Unit> QuitCommand { get; set; }
        public ReactiveCommand<Unit, Unit> RefreshCommand { get; set; }
        public ReactiveCommand<Unit, Unit> PrevPageCommand { get; set; }
        public ReactiveCommand<Unit, Unit> NextPageCommand { get; set; }

        protected IMAPClient client;



        [Reactive]
        public string CurrentPageIndexStr { get; set; }

        private int? _actualPageIndex;
        private int? ActualPageIndex {
            get => _actualPageIndex;
            set {
                //_actualPageIndex = value;
                int? validValue = value;

                if (value == null)
                {
                    // clear the page
                    CurrentMailList.Clear();
                    CurrentPageIndexStr = String.Empty;
                }
                else
                {
                    int maxPage = MailBoxes[SelectedMailboxIdx].MaxPages;
                    if (maxPage == 0)
                    {
                        validValue = null;
                        CurrentPageIndexStr = String.Empty;
                    }
                    else { 
                        if (value < 0) {
                            validValue = 0;
                        }
                        else if (value >= maxPage)
                        {
                            validValue = maxPage - 1;
                        }
                        CurrentPageIndexStr = validValue.Value.ToString();
                    }
                }
                // only the valid value for the current
                // mailbox can be set to the _actualPageIndex
                this.RaiseAndSetIfChanged(ref _actualPageIndex, validValue);
            }
        }

        /*        private int _selectedMailboxIdx;
                public int SelectedMailboxIdx
                {
                    get => _selectedMailboxIdx;
                    set
                    {
                        // set the new contents of the mailbox mail list
                        SelectMailboxAsync(MailBoxes[value]);
                        this.RaiseAndSetIfChanged(ref _selectedMailboxIdx, value);
                    }
                }*/

        [Reactive]
        public int SelectedMailboxIdx { get; set; }

        [Reactive]
        public string CurrentContent { get; set; }
        public ObservableCollection<MailMessage> CurrentMailList { get; set; } = new ObservableCollection<MailMessage>();

        public ObservableCollection<MailBox> MailBoxes { get; set; } = new ObservableCollection<MailBox>();

        public IMAPClientViewModel(IMAPClient client)
        {
            GlobalEnabler = true;

            this.client = client;

            // acquiring the list of mailbox and making the 
            // initially selected mailbox an empty one
            // UPD: the constructor cannot be async.
            // initially the mailbox combobox will be empty
            //GetListOfMailboxesAsync();


            // as the properties cannot be async:
            this.WhenAnyValue(x => x.SelectedMailboxIdx)
                .Subscribe(async idx =>
                {
                    await SelectMailboxAsync(idx);
                });

            // same here
            this.WhenAnyValue(x => x.ActualPageIndex)
                .Subscribe(
                    // here I must update the current mail list
                    // and query the mail header if needed
                );


            IObservable<bool> globalEnabledObserver = this.WhenAnyValue(x => x.GlobalEnabler);
            QuitCommand = ReactiveCommand.CreateFromTask(client.QuitSessionAsync, globalEnabledObserver);
            RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync, globalEnabledObserver);
            PrevPageCommand = ReactiveCommand.Create(PrevPageHandler, globalEnabledObserver);
            NextPageCommand = ReactiveCommand.Create(NextPageHandler, globalEnabledObserver);

            ActualPageIndex = null;

            this.WhenAnyValue(x => x.CurrentPageIndexStr)
                .Throttle(TimeSpan.FromMilliseconds(1000))
                .Subscribe(
                (string? v) => {
                    if (GlobalEnabler)
                    {
                        int idxValue;
                        if (int.TryParse(v, out idxValue))
                        {

                            // probably (and again, probably) this condition
                            // can be discarded because of .raiseandsetIFCHANGED
                            // but then the setter will be called, so no, thanks
                            if (ActualPageIndex != idxValue)
                            {
                                ActualPageIndex = idxValue;
                            }

                        }
                    }
                });
        }


        public void PrevPageHandler()
        {
            ActualPageIndex--;
        }
        public void NextPageHandler()
        {
            ActualPageIndex++;
        }

        public async Task RefreshAsync() {
            MailBoxes.Clear();
            List<MailBox> boxes = await Query_ListMailboxesAsync();
            MailBoxes.Add(new MailBox(MAILBOX_ALT_EMPTY, MAILBOX_EMPTY));
            MailBoxes.AddRange(boxes);
            //SelectedMailboxIdx = 0;
        }

        protected async Task SelectMailboxAsync(int mailboxID) {
            GlobalEnabler = false;

            // as the value for the first-time is initialized like so:
            if (mailboxID < MailBoxes.Count)
            {
                MailBox mb = MailBoxes[mailboxID];
                // checking if the current mailbox is a default one
                // p.s. the check can be simplified
                if (!(mb.Name == MAILBOX_EMPTY && mb.altName == MAILBOX_ALT_EMPTY))
                {
                    string[] selectResponse = await Query_SelectMailboxAsync(mb.Name);
                    if (selectResponse.Length > 0)
                    {
                        int amntOfMail = -1;
                        foreach (string response in selectResponse)
                        {
                            if (response.Contains("EXISTS"))
                            {
                                // assuming that the string contains data of type: 
                                // "* NNN EXISTS"
                                string pattern = @"\* (\d+) EXISTS";
                                Match match = Regex.Match(response, pattern);
                                if (match.Success)
                                {
                                    amntOfMail = int.Parse(match.Groups[1].Value);
                                }
                                else
                                {
                                    // error: string of other type
                                }
                                break;
                            }
                        }
                        if (amntOfMail == -1)
                        {
                            // error: EXISTS was not found
                        }

                        if (!(mb.MailCount != null && mb.MailCount == amntOfMail))
                        {
                            mb.MailCount = amntOfMail;
                            // now the max amount of pages is set
                        }

                        // display page; it'll deal with the unknown mail
                        //DisplayMailPageAsync(mb, 0);

                        // change the current page to 0?
                        if (amntOfMail == 0)
                        {
                            ActualPageIndex = null;
                        }
                        else
                        {
                            ActualPageIndex = 0;
                        }
                    }
                    else
                    {
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


            }

            GlobalEnabler = true;
        }

        protected async Task DisplayMailPageAsync(MailBox mb, int index) {
            GlobalEnabler = false;
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
                        MailMessage[] mail = await Query_FetchMessageAsync(startIdx, endIdx);
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

            GlobalEnabler = true;
        }



        public async Task<List<MailBox>> Query_ListMailboxesAsync()
        {
            QueryResult qResult;

            string query = $"LIST \"\" *";
            qResult = await client.SmalltalkAsync(query);

            if (!qResult.status)
            {
                // error occurred
                return null;
            }
            else
            {
                // get the current response as a string
                List<MailBox> boxes = new List<MailBox>();

                // update the combobox
                foreach (string line in qResult.data)
                {
                    if (line.StartsWith("* LIST "))
                    {
                        // current line is a mail box

                        string pattern = @"\* LIST \(([^)]*)\) ""([^""]*)"" ""([^""]*)""";
                        Match match = Regex.Match(line, pattern);
                        if (match.Success)
                        {
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



                // add to console log
                Log.Add(new DialogueStruct(qResult.header + " " + query, qResult.data));
                return boxes;
            }
        }

        protected async Task<MailMessage[]> Query_FetchMessageAsync(int startIdx, int endIdx) {
            QueryResult qResult;

            string query = $"FETCH {startIdx}:{endIdx} (BODY[HEADER.FIELDS (SUBJECT DATE)])";
            qResult = await client.SmalltalkAsync(query);

            if (!qResult.status)
            {
                // error occurred
                return [];
            }
            else
            {
                string[] data = qResult.data;

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

        protected async Task<int[]> Query_SearchAllAsync()
        {
            QueryResult qResult;

            string query = "SEARCH ALL";
            qResult = await client.SmalltalkAsync(query);

            if (!qResult.status)
            {
                // error occurred
                return [];
            }
            else
            {
                string[] data = qResult.data;
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

        public async Task<string[]> Query_SelectMailboxAsync(string boxName) {
            QueryResult qResult;

            string query = $"SELECT {boxName}";
            qResult = await client.SmalltalkAsync(query);

            if (!qResult.status)
            {
                // error occurred
                return [];
            }
            else {
                string[] data = qResult.data;
                int counter = 0;
                foreach (string line in data) {
                    Debug.WriteLine($"{counter}: {line}");
                    counter++;
                }
                return data;
            }
        }

        public async Task Query_GetDateSubjectAsync(int startIdx, int? endIdx = null) {
            QueryResult qResult;
            
            string addPart = endIdx == null ? "" : $":{endIdx}";
            string query = $"FETCH {startIdx}{addPart} (BODY[HEADER.FIELDS (SUBJECT DATE)])";

            qResult = await client.SmalltalkAsync(query);
            if (!qResult.status)
            {
                // error occurred
            }
            else
            {
                string[] data = qResult.data;
                // display data in a clickable listbox
                int counter = 0;
                foreach (string line in data) {
                    Debug.WriteLine($"{counter}: {line}");
                    counter++;
                }
            }
        }

        public async Task Query_GetMailContentAsync(int msgIdx) {
            QueryResult qResult;

            string query = $"FETCH {msgIdx} (BODY[TEXT])";
            qResult = await client.SmalltalkAsync(query);
            if (!qResult.status)
            {
                // error occurred
            }
            else
            {
                string[] data = qResult.data;
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
