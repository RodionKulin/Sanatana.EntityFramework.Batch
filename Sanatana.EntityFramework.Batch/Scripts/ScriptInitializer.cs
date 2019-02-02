using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.Interception;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace Sanatana.EntityFramework.Batch.Scripts
{
    /// <summary>
    /// Database initializer that executes sql scripts on database creation.
    /// </summary>
    /// <typeparam name="T">DbContext</typeparam>
    public class ScriptInitializer<T> : CreateDatabaseIfNotExists<T>
        where T : DbContext
    {
        //properties
        public Dictionary<string, string> Replacements { get; set; }
        public ScriptManager ScriptManager { get; private set; }


        //init
        public ScriptInitializer(DirectoryInfo scriptDirectory, Dictionary<string, string> replacements = null)
        {
            ScriptManager = new ScriptManager(scriptDirectory);
            Replacements = replacements;
        }
       
        public ScriptInitializer(Type scriptResourceType, Dictionary<string, string> replacements = null)
        {
            ScriptManager = new ScriptManager(scriptResourceType);
            Replacements = replacements;
        }


        //methods
        public override void InitializeDatabase(T context)
        {
            bool created = context.Database.CreateIfNotExists();

            if (created)
            {
                ExecuteScripts(context);
                
                Seed(context);
            }
        }
        
        public virtual void ExecuteScripts(T context)
        {
            var replacements = Replacements ?? new Dictionary<string, string>();

            foreach (SqlScript item in ScriptManager.Scripts)
            {
                item.Replacement = replacements;
            }

            ScriptManager.ExecuteScripts(context);
        }
    }
}