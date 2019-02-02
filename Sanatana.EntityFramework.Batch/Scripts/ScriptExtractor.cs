using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Scripts
{
    public class ScriptExtractor
    {
        public static void ExtractFromDbContext(DbContext context)
        {
            string script = ((IObjectContextAdapter)context).ObjectContext.CreateDatabaseScript();

            bool connectionOpened = false;
            if (context.Database.Connection.State != System.Data.ConnectionState.Open)
            {
                context.Database.Connection.Open();
                connectionOpened = true;
            }
                        
            context.Database.ExecuteSqlCommand(script);
            
            if (connectionOpened)
            {
                context.Database.Connection.Close();
            }
        }
    }
}
