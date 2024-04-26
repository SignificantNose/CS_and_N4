using CS_and_N4.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS_and_N4.ViewModels
{

    public class MailMessage {
        public string msgHeader { get; set; }
        public string? msgDate { get; set; }
        public string? msgBody { get; set; }

        public MailMessage(string header, string? date = null, string? body = null)
        {
            msgHeader = header;
            msgDate = date;
            msgBody = body;
        }
    }
    public class MailBox
    {
        public static readonly int mailPerPage = 5;
        public string Name { get; set; }
        public string altName { get; set; }

        // this field is responsible for giving the answer if the
        // SELECT command has been queried
        // UPD: this is not necessary, as the mail that will be 
        // displayed will contain the mail for the currently
        // selected mailbox
        // UPD UPD: the IDs must be known anyway
        // UPD UPD UPD: if the dictionary keys are empty, that is the sign
        //public int? amntOfMail = null;


        public int MaxPages { get; private set; }

        private int? _mailCount;
        public int? MailCount { 
            get => _mailCount; 
            set { 
                _mailCount = value;
                if (value != null)
                {
                    MaxPages = (int)Math.Ceiling((double)value / mailPerPage);
                }
                else {
                    // not valid value for MailCount to acquire MaxPages
                    MaxPages = 0;
                }
            } 
        }

        public Dictionary<int,MailMessage?> Mail = new Dictionary<int,MailMessage?>();


        public MailBox(string Name, string altName)
        {
            this.Name = Name;
            this.altName = altName;
            Mail = new Dictionary<int,MailMessage?>();
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



    public abstract class MailClientViewModelBase : ViewModelBase
    {
        protected static readonly string MAILBOX_EMPTY = "CSN4_NONE";
        protected static readonly string MAILBOX_ALT_EMPTY = "CSN4_NONE";

        public ObservableCollection<DialogueStruct> Log { get; set; } = new ObservableCollection<DialogueStruct>();
    }
}
