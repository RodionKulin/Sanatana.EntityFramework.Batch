using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Scripts
{
    public class SqlScript
    {
        //properties
        public string ScriptFullName { get; set; }
        public string ScriptText { get; set; }
        public Dictionary<string, string> Replacement { get; set; }


        //dependent properties
        internal int Order { get; private set; }
        internal SqlObjectType SqlObjectType { get; private set; }
        internal string SqlObjectName { get; set; }




        //init
        public SqlScript(string scriptText, string scriptFullName)
        {
            ScriptText = scriptText;

            Regex orderedListRegex = new Regex(@"\d+-.*");
            string name = scriptFullName;
            if (orderedListRegex.IsMatch(name))
            {
                string[] nameParts = scriptFullName.Split('-');
                Order = int.Parse(nameParts[0]);
                name = nameParts[1];
            }

            ScriptFullName = name;

            SqlObjectType = DetermineType(ScriptText);
            SqlObjectName = DetermineObjectName(ScriptText);

            if (SqlObjectType != SqlObjectType.AlterTable && SqlObjectName == null)
                SqlObjectType = SqlObjectType.Unknown;
        }


        //methods
        /// <summary>
        /// Find script type from it's content
        /// </summary>
        /// <param name="scriptContent">script</param>
        /// <returns></returns>
        private SqlObjectType DetermineType(string scriptContent)
        {
            string line = null;
            using (StringReader reader = new StringReader(scriptContent))
            {
                line = reader.ReadLine();
            } 

            if (!string.IsNullOrEmpty(line))
            {
                line = line.ToLower();

                string[] words = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length < 3)
                    return SqlObjectType.Unknown;
                
                if (words[0] == "alter" && words[1] == "table")
                    return SqlObjectType.AlterTable;

                if (words[0] != "create")
                    return SqlObjectType.Unknown;

                if (words[1] == "table")
                    return SqlObjectType.Table;

                Regex indexRegex = new Regex(@"CREATE(\s*UNIQUE)?(\s*(CLUSTERED|NONCLUSTERED))?\s*INDEX"
                    , RegexOptions.IgnoreCase);
                if (indexRegex.IsMatch(line))
                    return SqlObjectType.Index;

                if ((words[1] == "stored" && words[2] == "procedure")
                    || words[1] == "procedure"
                    || words[1] == "proc")
                    return SqlObjectType.StoredProcedure;

                if (words[1] == "function")
                    return SqlObjectType.Function;

                Regex tableTypeRegex = new Regex(@"TYPE\s\[dbo\].\[.*?\]\sAS\sTABLE", RegexOptions.IgnoreCase);
                if (tableTypeRegex.IsMatch(line))
                    return SqlObjectType.TableType;
            }           

            return SqlObjectType.Unknown;
        }

        /// <summary>
        /// Find created object name from content
        /// </summary>
        /// <param name="scriptContent">script</param>
        /// <returns></returns>
        private string DetermineObjectName(string scriptContent)
        {
            string objectName = null;

            if (SqlObjectType == SqlObjectType.Index)
            {
                Regex objectNameRegex = new Regex(@"index\s\[(.*?)\]", RegexOptions.IgnoreCase);
                Match match = objectNameRegex.Match(scriptContent);
                if (match.Success)
                {
                    objectName = match.Groups[1].Value.ToLower();
                }
            }
            else
            {
                Regex objectNameRegex = new Regex(@"\s\[dbo\].\[(.*?)\]", RegexOptions.IgnoreCase);
                Match match = objectNameRegex.Match(scriptContent);
                if (match.Success)
                {
                    objectName = match.Groups[1].Value.ToLower();
                }
            }

            return objectName;
        }
    }
}
