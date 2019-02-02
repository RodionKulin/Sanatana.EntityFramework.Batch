using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Scripts
{
    public class ScriptManager
    {
        //fields
        private LoadType _loadType;
        private DirectoryInfo _scriptDirectory;
        private Type _scriptResourceType;
        private static Dictionary<SqlObjectType, bool> _executeOrder = new Dictionary<SqlObjectType, bool>
        {
            { SqlObjectType.Table, true },
            { SqlObjectType.AlterTable, false },
            { SqlObjectType.Index, true },
            { SqlObjectType.TableType, true },
            { SqlObjectType.Function, true },
            { SqlObjectType.StoredProcedure, true },
            { SqlObjectType.Unknown, false },
        };
        

        //properties
        public bool ThrowOnUnknownScriptTypes { get; set; }

        public List<SqlScript> Scripts { get; set; }



        //init
        public ScriptManager(Type scriptResourceType)
        {
            _loadType = LoadType.Resource;
            _scriptResourceType = scriptResourceType;
            LoadScripts();
        }

        public ScriptManager(DirectoryInfo scriptDirectory)
        {
            _loadType = LoadType.Folder;
            _scriptDirectory = scriptDirectory;
            LoadScripts();
        }

        public ScriptManager(List<SqlScript> scripts)
        {
            _loadType = LoadType.NoLoad;
        }


        //get scripts
        private void LoadScripts()
        {
            List<SqlScript> scripts = _loadType == LoadType.Folder
                ? LoadFromFolder(_scriptDirectory)
                : LoadFromResource(_scriptResourceType);

            ValidateScripts(scripts);

            Scripts = scripts.OrderBy(p => p.Order).ToList();            
        }

        private List<SqlScript> LoadFromFolder(DirectoryInfo directory)
        {
            List<SqlScript> scripts = new List<SqlScript>();            
            if (!directory.Exists)
                return scripts;

            FileInfo[] scriptFiles = directory.GetFiles("*.sql");

            foreach (FileInfo file in scriptFiles)
            {
                string striptName = Path.GetFileNameWithoutExtension(file.Name);
                string scriptText = File.ReadAllText(file.FullName);
                SqlScript script = new SqlScript(scriptText, striptName);
                scripts.Add(script);
            }

            DirectoryInfo[] subfolders = directory.GetDirectories();
            foreach (DirectoryInfo subfolder in subfolders)
            {
                List<SqlScript> subfolderScripts = LoadFromFolder(subfolder);
                scripts.AddRange(subfolderScripts);
            }

            return scripts;
        }

        private List<SqlScript> LoadFromResource(Type resourceType)
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            PropertyInfo resourceManagerProp = resourceType
                .GetProperty("ResourceManager", flags);
            ResourceManager resourceManager = (ResourceManager)resourceManagerProp.GetValue(null);

            ResourceSet set = resourceManager
                .GetResourceSet(CultureInfo.InvariantCulture, true, true);

            Type stringType = typeof(string);
            List<SqlScript> scripts = new List<SqlScript>();

            foreach (DictionaryEntry entry in set)
            {
                if (entry.Value.GetType() == stringType)
                {
                    SqlScript script = new SqlScript((string)entry.Value, (string)entry.Key);
                    scripts.Add(script);
                }
            }

            return scripts;
        }

        private bool ValidateScripts(List<SqlScript> scripts)
        {
            bool hasUnknown = scripts.Any(p => p.SqlObjectType == SqlObjectType.Unknown);

            if (ThrowOnUnknownScriptTypes && hasUnknown)
            {
                SqlScript unknownScript = scripts.First(p => p.SqlObjectType == SqlObjectType.Unknown);
                string exceptionMessage = $"Was not able to determine type of script {unknownScript.ScriptFullName}.";
                throw new Exception(exceptionMessage);
            }

            return hasUnknown;
        }


        //execution
        public void ExecuteScripts(DbContext dbContext)
        {
            //replace fragments
            foreach (SqlScript script in Scripts)
            {
                if (script.Replacement == null)
                    continue;

                foreach (KeyValuePair<string,string> item in script.Replacement)
                {
                    if (script.ScriptText != null)
                        script.ScriptText = script.ScriptText.Replace(item.Key, item.Value);

                    if(script.SqlObjectName != null)
                        script.SqlObjectName = script.SqlObjectName.Replace(item.Key, item.Value);
                }
            }

            // execute scripts
            bool connectionOpened = false;
            if (dbContext.Database.Connection.State != System.Data.ConnectionState.Open)
            {
                dbContext.Database.Connection.Open();
                connectionOpened = true;
            }

            
            foreach (KeyValuePair<SqlObjectType, bool> sqlTypeOrder in _executeOrder)
            {
                SqlObjectType scriptType = sqlTypeOrder.Key;
                if (sqlTypeOrder.Value)
                {
                    ExecuteOfTypeIfNotExists(dbContext, scriptType);
                }
                else
                {
                    ExecuteOfType(dbContext, scriptType);
                }
            }

            if (connectionOpened)
            {
                dbContext.Database.Connection.Close();
            }
        }

        private void ExecuteOfTypeIfNotExists(DbContext dbContext, SqlObjectType sqlObjectType)
        {
            List<SqlScript> filteredScripts = Scripts
                .Where(p => p.SqlObjectType == sqlObjectType).ToList();

            if (filteredScripts.Count > 0)
            {
                List<string> existingNames = GetSqlNamesOfType(dbContext, sqlObjectType);

                foreach (SqlScript script in filteredScripts)
                {
                    try
                    {
                        if (!existingNames.Contains(script.SqlObjectName))
                        {
                            dbContext.Database.ExecuteSqlCommand(script.ScriptText);
                        }
                    }
                    catch (Exception exception)
                    {
                        string message = exception.Message + $" Executing script: {script.ScriptText}";
                        throw new Exception(message);
                    }
                }
            }
        }

        private void ExecuteOfType(DbContext dbContext, SqlObjectType sqlObjectType)
        {
            List<SqlScript> filteredScripts = Scripts
                .Where(p => p.SqlObjectType == sqlObjectType).ToList();

            if (filteredScripts.Count > 0)
            {
                foreach (SqlScript script in filteredScripts)
                {
                    try
                    {
                        dbContext.Database.ExecuteSqlCommand(script.ScriptText);
                    }
                    catch (Exception exception)
                    {
                        string message = exception.Message + $" Executing script: {script.ScriptText}";
                        throw new Exception(message);
                    }
                }
            }
        }


        //get existing db objects
        private List<string> GetSqlNamesOfType(DbContext dbContext, SqlObjectType sqlObjectType)
        {
            if (sqlObjectType == SqlObjectType.Index)
            {
                return GetSqlNamesOfIndexes(dbContext);
            }

            string sqlTypeString = ConvertSqlObjectType(sqlObjectType);
            string query = "select * from sysobjects where type='" + sqlTypeString + "'";
            List<string> existingItems = new List<string>();

            using (SqlCommand command = new SqlCommand(query, (SqlConnection)dbContext.Database.Connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        string nameString = (string)reader[0];
                        existingItems.Add(nameString.ToLower());
                    }
                }
            }

            //Table Type get actual names
            Regex tableTypeRegex = new Regex(@"tt_([\d\w]+)_[\d\w]+", RegexOptions.IgnoreCase);
            if (sqlObjectType == SqlObjectType.TableType)
            {
                for (int i = 0; i < existingItems.Count; i++)
                {
                    Match match = tableTypeRegex.Match(existingItems[i]);
                    if (match != null)
                        existingItems[i] = match.Groups[1].Value;
                }
            }

            return existingItems;
        }

        private List<string> GetSqlNamesOfIndexes(DbContext context)
        {
            string query = "select * from sys.indexes";
            List<string> existingItems = new List<string>();

            using (SqlCommand command = new SqlCommand(query, (SqlConnection)context.Database.Connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (!reader.IsDBNull(1))
                    {
                        string nameString = (string)reader[1];
                        existingItems.Add(nameString.ToLower());
                    }
                }
            }

            return existingItems;
        }

        private string ConvertSqlObjectType(SqlObjectType SqlObjectType)
        {
            if (SqlObjectType == SqlObjectType.Function)
                return "FN' OR type='TF";
            else if (SqlObjectType == SqlObjectType.StoredProcedure)
                return "P";
            else if (SqlObjectType == SqlObjectType.Table)
                return "U";
            else if (SqlObjectType == SqlObjectType.TableType)
                return "TT";
            else
                throw new NotImplementedException("Unexpected SqlObjectType value");

            // all types -  http://technet.microsoft.com/ru-ru/library/ms177596.aspx
            //FN = scalar function
            //TF = table function
            //string query = "select * from sysobjects where type='FN" + "' OR type='TF"  + "'";
            //FN' OR type='TF
        }


        //Checks
        public bool CheckTableExists(DbContext context
            , string checkExistingTableSchema, string checkExistingTableName)
        {
            bool exists = context.Database
                      .SqlQuery<int?>($@"
                         SELECT 1 FROM sys.tables AS T
                         INNER JOIN sys.schemas AS S ON T.schema_id = S.schema_id
                         WHERE S.Name = '{checkExistingTableSchema}' AND T.Name = '{checkExistingTableName}'")
                      .SingleOrDefault() != null;

            return exists;
        }

        public bool CheckProcedureExists(DbContext context
            , string checkExistingProcedureSchema, string checkExistingProcedureName)
        {
            bool exists = context.Database
                      .SqlQuery<int?>($@"
                            SELECT 1 FROM sysobjects
                            WHERE  id = object_id(N'[{checkExistingProcedureSchema}].[{checkExistingProcedureName}]') 
                            AND OBJECTPROPERTY(id, N'IsProcedure') = 1")
                      .SingleOrDefault() != null;

            return exists;
        }
    }
}
