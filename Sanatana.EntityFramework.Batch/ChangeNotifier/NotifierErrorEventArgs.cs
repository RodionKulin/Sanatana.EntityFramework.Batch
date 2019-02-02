using System;
using System.Data.SqlClient;

namespace Sanatana.EntityFramework.Batch.ChangeNotifier
{
    public class NotifierErrorEventArgs : EventArgs
    {
        public string Sql { get; set; }
        public SqlNotificationEventArgs Reason { get; set; }
    }
}
