using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CornBot.Models
{
    public class UserHistory
    {

        public enum ActionType
        {
            MESSAGE,
            DAILY
        }

        public struct HistoryEntry
        {
            public ulong Id;
            public ulong UserId;
            public ActionType Type;
            public long Value;
        }

        public UserInfo User { get; init; }
        public List<HistoryEntry> Entries { get; init; }

        public UserHistory(UserInfo user, List<HistoryEntry> entries)
        {
            User = user;
            Entries = entries;
        }

        public UserHistory(UserInfo user) : this(user, new())
        { }

        public void AddAction(HistoryEntry entry)
        {
            this.Entries.Add(entry);
        }
    }
}
