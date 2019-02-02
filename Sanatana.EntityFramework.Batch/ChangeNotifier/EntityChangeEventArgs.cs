using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Sanatana.EntityFramework.Batch.ChangeNotifier
{
    public class EntityChangeEventArgs<T> : EventArgs
    {
        public bool ContinueListening { get; set; }
        public SqlNotificationType Type { get; set; }
        public SqlNotificationInfo Info { get; set; }
    }
}
