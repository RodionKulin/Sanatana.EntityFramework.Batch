using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.ChangeNotifier
{

    public class EntityChangeNotifier<TEntity>
        where TEntity : class
    {
        //fields
        private static List<string> _connectionStrings;
        private DbQuery<TEntity> _query;
        private string _connectionString;


        //events
        public event EventHandler<EntityChangeEventArgs<TEntity>> Changed;
        public event EventHandler<NotifierErrorEventArgs> Error;


        //init
        static EntityChangeNotifier()
        {
            _connectionStrings = new List<string>();

            AppDomain.CurrentDomain.ProcessExit += StopSqlDependecy;
        }

        public EntityChangeNotifier(DbQuery<TEntity> query, string connectionString)
        {
            _query = query;
            _connectionString = connectionString;

            SqlDependency.Start(_connectionString);
            _connectionStrings.Add(_connectionString);
        }


        //methods
        public virtual void RegisterNotification()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = _query.ToSqlCommand())
            {
                command.Connection = connection;
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                var sqlDependency = new SqlDependency(command);
                sqlDependency.OnChange += new OnChangeEventHandler(SqlDependency_OnChange);

                // NOTE: You have to execute the command, or the notification will never fire.
                using (SqlDataReader reader = command.ExecuteReader())
                {
                }
            }
        }


        //event handling
        protected void SqlDependency_OnChange(object sender, SqlNotificationEventArgs e)
        {
            SqlDependency sqlDependency = sender as SqlDependency;
            sqlDependency.OnChange -= new OnChangeEventHandler(SqlDependency_OnChange);

            if (e.Type == SqlNotificationType.Subscribe || e.Info == SqlNotificationInfo.Error)
            {
                var args = new NotifierErrorEventArgs
                {
                    Reason = e,
                    Sql = _query.ToString()
                };

                if (Error != null)
                {
                    Error(this, args);
                }
            }
            else
            {
                var args = new EntityChangeEventArgs<TEntity>
                {
                    ContinueListening = true,
                    Type = e.Type,
                    Info = e.Info
                };

                if (Changed != null)
                {
                    Changed(this, args);
                }

                if (args.ContinueListening)
                {
                    RegisterNotification();
                }
            }
        }


        //disposing
        protected static void StopSqlDependecy(object sender, EventArgs e)
        {
            foreach (string cs in _connectionStrings)
            {
                SqlDependency.Stop(cs);
            }
        }
    }
}
