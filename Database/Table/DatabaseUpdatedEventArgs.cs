using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Table
{
    public class DatabaseUpdatedEventArgs
    {
        public object Sender { get; set; }
        public DatabaseAction Action { get; set; }
    }

    public enum DatabaseAction
    {
        Insert,
        Delete,
        Update,
        Refresh,
    }
}
