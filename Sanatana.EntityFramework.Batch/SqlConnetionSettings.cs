using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch
{
    public class SqlConnetionSettings
    {
        //properties
        public virtual string NameOrConnectionString { get; set; }
        public virtual string Schema { get; set; }


        //init
        public SqlConnetionSettings()
        {

        }
        public SqlConnetionSettings(string nameOrConnectionString, string schema)
        {
            NameOrConnectionString = nameOrConnectionString;
            Schema = schema;
        }
    }
}
