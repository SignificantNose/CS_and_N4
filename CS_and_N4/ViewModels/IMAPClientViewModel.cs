﻿using Avalonia.Threading;
using CS_and_N4.Models;
using DynamicData;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MimeKit.Tnef;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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


        public ReactiveCommand<string?, string?> QuitCommand { get; set; }
        public ReactiveCommand<Unit, Unit> RefreshCommand { get; set; }
        public ReactiveCommand<Unit, Unit> PrevPageCommand { get; set; }
        public ReactiveCommand<Unit, Unit> NextPageCommand { get; set; }

        protected IMAPClient client;



        [Reactive]
        public string CurrentPageIndexStr { get; set; }

        [Reactive]
        public int ChosenLogItemIdx { get; set; }

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

        [Reactive]
        public int SelectedMailIdx { get; set; }

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

        private void HandleError(string errorMsg) {
            QuitCommand.Execute(errorMsg);                   
        }

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
                .Subscribe(async (x) => {

                    // here I must update the current mail list
                    // and query the mail header if needed
                    await DisplayMailPageAsync(SelectedMailboxIdx, x);
                });

            this.WhenAnyValue(x => x.ChosenLogItemIdx)
                .Subscribe((logIdx) =>
                    {
                        if (logIdx < Log.Count && logIdx >= 0)
                        {
                            SelectedMailIdx = -1;
                            DialogueStruct currLogElem = Log[logIdx];
                            CurrentContent = "Query: " + currLogElem.Query + "\r\nResponse: " + currLogElem.Response;
                        }
                    }
                );


            this.WhenAnyValue(x => x.SelectedMailIdx)
                .Subscribe(
                async (mailIdx) =>
                {
                    // WHAT??
                    if (GlobalEnabler && mailIdx >= 0 && mailIdx < CurrentMailList.Count && ActualPageIndex.HasValue)
                    {
                        ChosenLogItemIdx = -1;
                        await ShowMailAsync(MailBoxes[SelectedMailboxIdx], ActualPageIndex.Value, mailIdx);
                    }
                }
                );


            IObservable<bool> globalEnabledObserver = this.WhenAnyValue(x => x.GlobalEnabler);
            QuitCommand = ReactiveCommand.CreateFromTask<string?, string?>(client.InitiateQuitAsync, globalEnabledObserver);
            RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync, globalEnabledObserver);
            PrevPageCommand = ReactiveCommand.Create(PrevPageHandler, globalEnabledObserver);
            NextPageCommand = ReactiveCommand.Create(NextPageHandler, globalEnabledObserver);

            ActualPageIndex = null;
            ChosenLogItemIdx = -1;
            SelectedMailIdx = -1;


            // because the throttle is not UI-thread-related (?),
            // I need to manually invoke a method from the UI-thread
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
                                Dispatcher.UIThread.Post(() => ActualPageIndex = idxValue);
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

        // pageIdx = 
        protected async Task ShowMailAsync(MailBox mb, int pageIdx, int mailPageIdx) {
            GlobalEnabler = false;

            int mailIdx = pageIdx * MailBox.mailPerPage + mailPageIdx + 1;
            if (mb.MailCount >= mailIdx)
            {
                // good
                if (!mb.Mail.ContainsKey(mailIdx))
                {
                    // error
                    HandleError("Implementation error: mail key not found in mail");
                }
                else {
                    if (mb.Mail[mailIdx].msgBody == null) {
                        string? body = await Query_GetMailContentAsync(mailIdx);
                        if (body == null)
                        {
                            // error
                        }
                        else {
                            mb.Mail[mailIdx].msgBody = body;
                        }
                    }

                    MailMessage msg = mb.Mail[mailIdx];
                    // display the message
                    CurrentContent = $"Subject: {msg.msgHeader}\r\nDate: {msg.msgDate}\r\nBody:\r\n{msg.msgBody}";
                }
            }
            else {
                // invalid idx
                HandleError("Implementation error: invalid key for message");
            }
            

            GlobalEnabler = true;
        }

        protected async Task SelectMailboxAsync(int mailboxID) {
            GlobalEnabler = false;

            ActualPageIndex = null;
            // as the value for the first-time is initialized like so:
            if (mailboxID >= 0 && mailboxID < MailBoxes.Count)
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
                                break;
                            }
                        }
                        if (amntOfMail == -1)
                        {
                            // error: EXISTS was not found
                            HandleError("Implementation error: string of type \"* NNN EXISTS\" expected. Other string received");
                        }

                        if (!(mb.MailCount != null && mb.MailCount == amntOfMail))
                        {
                            mb.MailCount = amntOfMail;
                            // now the max amount of pages is set
                        }

                        // display page; it'll deal with the unknown mail
                        //DisplayMailPageAsync(mb, 0);

                        ActualPageIndex = null;
                        // change the current page to 0?
                        if (amntOfMail != 0)
                        {
                            ActualPageIndex = 0;
                        }
                    }
                    else
                    {
                        // error in response. already handled directly by the query maker
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

        protected async Task DisplayMailPageAsync(int mailboxIdx, int? index) {
            GlobalEnabler = false;
            CurrentMailList.Clear();

            if (index != null && mailboxIdx < MailBoxes.Count)
            {
                MailBox mb = MailBoxes[mailboxIdx];
                if (mb.MailCount != null)
                {
                    int maxPageIdx = mb.MaxPages - 1;
                    if (index > maxPageIdx) index = maxPageIdx;
                    if (index < 0) index = 0;

                    int startIdx = index.Value * MailBox.mailPerPage + 1;
                    int endIdx = (index.Value + 1) * MailBox.mailPerPage;

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
                            if (mail.Length != endIdx - startIdx + 1)
                            {
                                // error: some messages are not parsed
                                HandleError("Error: some messages could not have been parsed properly");
                            }
                            else
                            {
                                for (int j = startIdx; j <= endIdx; j++)
                                {
                                    if (mb.Mail.ContainsKey(j))
                                    {
                                        mb.Mail[j] = mail[j - startIdx];
                                    }
                                    else
                                    {
                                        mb.Mail.Add(j, mail[j - startIdx]);
                                    }
                                }
                            }

                            break;
                        }
                    }

                    for (int i = startIdx; i <= endIdx; i++)
                    {
                        currMsgRange.Add(mb.Mail[i]);
                    }

                    CurrentMailList.AddRange(currMsgRange);
                }
                else
                {
                    // unexpected error
                    HandleError("Implementation error: Mailbox doesn't contain mail amount");
                }
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
                HandleError($"LIST failed: {qResult.header}");
                return null;
            }
            else
            {
                // get the current response as a string
                List<MailBox> boxes = new List<MailBox>();

                int lastRowIdx = qResult.data.Length - 1;
                if (qResult.data[lastRowIdx].StartsWith($"{qResult.header} OK"))
                {
                    // update the combobox
                    foreach (string line in qResult.data)
                    {
                        if (line.StartsWith("* LIST "))
                        {
                            // current line is a mail box

                            string pattern = @"\* LIST \(([^)]*)\) ""([^""]*)"" (.+)";
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
                }
                else
                {
                    HandleError(qResult.data[lastRowIdx]);
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
                HandleError($"FETCH headers failed: {qResult.header}");
                return [];
            }
            else
            {
                string[] data = qResult.data;
                List<MailMessage> mailMsgs = new List<MailMessage>();


                int lastRowIdx = qResult.data.Length - 1;
                if (qResult.data[lastRowIdx].StartsWith($"{qResult.header} OK"))
                {
                    string date = "";
                    string subject = "";
                    for (int i = 1; i < data.Length-1; i++) {
                        string currString = data[i];
                        if (currString == ")")
                        {
                            subject = MimeKit.Utils.Rfc2047.DecodeText(Encoding.UTF8.GetBytes(subject));
                            date = MimeKit.Utils.Rfc2047.DecodeText(Encoding.UTF8.GetBytes(date));
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
                                // bad
                                while (i+1 < data.Length-1 && !data[i + 1].StartsWith("Date: ") && !(data[i + 1] == "" && !(data[i+1]==")"))){
                                    currString = data[i+1];
                                    subject += currString;
                                    i++;
                                }
                            }
                        }
                    }
                }
                else
                {
                    HandleError(qResult.data[lastRowIdx]);
                }

       

                // take the date string, remove it from the string poll
                // the subject will be anything else

                Log.Add(new DialogueStruct(qResult.header + " " + query, qResult.data));
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
                HandleError($"SEARCH failed: {qResult.header}");
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

                Log.Add(new DialogueStruct(qResult.header + " " + query, qResult.data));

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
                HandleError($"SELECT failed: {qResult.header}");
                return [];
            }
            else {
                string[] data = [];
                int lastRowIdx = qResult.data.Length - 1;
                if (qResult.data[lastRowIdx].StartsWith($"{qResult.header} OK"))
                {
                    data = qResult.data;
                    int counter = 0;
                    foreach (string line in data) {
                        Debug.WriteLine($"{counter}: {line}");
                        counter++;
                    }

                    Log.Add(new DialogueStruct(qResult.header + " " + query, qResult.data));

                }
                else
                {
                    HandleError(qResult.data[lastRowIdx]);
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
                HandleError($"FETCH for headers failed: {qResult.header}");
            }
            else
            {
                int lastRowIdx = qResult.data.Length - 1;
                if (qResult.data[lastRowIdx].StartsWith($"{qResult.header} OK"))
                {
                    string[] data = qResult.data;
                    // display data in a clickable listbox
                    int counter = 0;
                    foreach (string line in data) {
                        Debug.WriteLine($"{counter}: {line}");
                        counter++;
                    }

                    Log.Add(new DialogueStruct(qResult.header + " " + query, qResult.data));
                }
                else {
                    HandleError(qResult.data[lastRowIdx]);
                }
            }
        }

        public async Task<string?> Query_GetMailContentAsync(int msgIdx) {
            QueryResult qResult;

            string query = $"FETCH {msgIdx} (BODY[TEXT])";
            qResult = await client.SmalltalkAsync(query);
            if (!qResult.status)
            {
                // error occurred
                HandleError($"FETCH for msg failed: {qResult.header}");
                return null;
            }
            else
            {
                string? response = null;
                int lastRowIdx = qResult.data.Length - 1;
                if (qResult.data[lastRowIdx].StartsWith($"{qResult.header} OK"))
                {
                    response = "";
                    for (int i = 1; i < lastRowIdx; i++) {
                        response += MimeKit.Utils.Rfc2047.DecodeText(Encoding.UTF8.GetBytes(qResult.data[i])) + "\r\n";
                    }
                    Log.Add(new DialogueStruct(qResult.header + " " + query, qResult.data));
                }
                else {
                    HandleError(qResult.data[lastRowIdx]);
                }
                return response;
            }
        }
    }
}
