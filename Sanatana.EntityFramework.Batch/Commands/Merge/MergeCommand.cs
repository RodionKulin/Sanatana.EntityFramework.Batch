using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sanatana.EntityFramework.Batch;
using Sanatana.EntityFramework.Batch.Expressions;
using Sanatana.EntityFramework.Batch.Reflection;
using System.Data;
using System.Data.SqlClient;
using Sanatana.EntityFramework.Batch.ColumnMapping;
using Sanatana.EntityFramework.Batch.Commands;

namespace Sanatana.EntityFramework.Batch.Commands.Merge
{
    public class MergeCommand<TEntity>
        where TEntity : class
    {
        //fields
        protected const string SOURCE_ID_COLUMN_NAME = "_SourceRowId";
        protected string _targetAlias;
        protected string _sourceAlias;
        protected MergeType _mergeType;
        protected bool _useTVP;
        protected List<TEntity> _entityList;
        protected DbContext _context;
        protected SqlTransaction _transaction;
        protected List<MappedProperty> _entityProperties;
        protected MappedPropertyUtility _mergePropertyUtility;


        //properties
        /// <summary>
        /// Type of the Table Valued Parameter that is expected to be already created on SQL server before executing merge, describing order or Source columns. 
        /// Required when using merge constructor with TVP, not required if using SqlParameters constructor.
        /// </summary>
        public string SqlTVPTypeName { get; set; }
        /// <summary>
        /// Name of the Table Valued Parameter that defaults to @Table. This can be any string.
        /// Required when using merge constructor with TVP, not required if using SqlParameters constructor.
        /// </summary>
        public string SqlTVPParameterName { get; set; }
        /// <summary>
        /// Target table name taken from EntityFramework settings by default. Can be changed manually.
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// List of columns to include as parameters to the query from provided Source entities.
        /// All properties are included by default.
        /// </summary>
        public MappingComponent<TEntity> Source { get; protected set; }
        /// <summary>
        /// List of columns used to match Target table rows to Source rows.
        /// All properties are excluded by default.
        /// </summary>
        public MergeComparePart<TEntity> Compare { get; protected set; }
        /// <summary>
        /// Used if Update or Upsert type of merge is executed.
        /// List of columns to update on Target table for rows that did match Source rows.
        /// All properties are included by default.
        /// </summary>
        public MergeUpdatePart<TEntity> UpdateMatched { get; protected set; }
        /// <summary>
        /// Used if Update type of merge is executed.
        /// List of columns to update on Target table for rows that did not match Source rows.
        /// All properties are excluded by default.
        /// </summary>
        public MergeUpdatePart<TEntity> UpdateNotMatched { get; protected set; }
        /// <summary>
        /// Used if Insert or Upsert type of merge is executed.
        /// List of columns to insert.
        /// All properties are included by default.
        /// </summary>
        public MergeInsertPart<TEntity> Insert { get; protected set; }
        /// <summary>
        /// List of columns to return for inserted rows. 
        /// Include columns that are generated on database side, like auto increment field.
        /// Returned values will be set to provided entities properties.
        /// Database generated or computed properties are included by default.
        /// </summary>
        public MappingComponent<TEntity> Output { get; protected set; }



        //init
        private MergeCommand(DbContext context, SqlTransaction transaction = null)
        {
            _context = context;
            _transaction = transaction;
            TableName = _context.GetTableName<TEntity>();

            Type entityType = typeof(TEntity);
            _mergePropertyUtility = new MappedPropertyUtility(context, entityType);
            _entityProperties = _mergePropertyUtility.GetAllEntityProperties();

            Source = new MappingComponent<TEntity>(_entityProperties, _mergePropertyUtility);
            Compare = new MergeComparePart<TEntity>(_entityProperties, _mergePropertyUtility)
            {
                ExcludeAllByDefault = true
            };
            UpdateMatched = new MergeUpdatePart<TEntity>(_entityProperties, _mergePropertyUtility);
            UpdateNotMatched = new MergeUpdatePart<TEntity>(_entityProperties, _mergePropertyUtility)
            {
                ExcludeAllByDefault = true
            };
            Insert = new MergeInsertPart<TEntity>(_entityProperties, _mergePropertyUtility)
            {
                IncludeGeneratedProperties = IncludeDbGeneratedProperties.ExcludeByDefault
            };
            Output = new MappingComponent<TEntity>(_entityProperties, _mergePropertyUtility)
            {
                ExcludeAllByDefault = true,
                IncludeGeneratedProperties = IncludeDbGeneratedProperties.IncludeByDefault
            };
            _targetAlias = ExpressionsToMSSql.ALIASES[0];
            _sourceAlias = ExpressionsToMSSql.ALIASES[1];
        }

        /// <summary>
        /// Merge entity into the table and pass values as SqlParameter.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <param name="transaction"></param>
        public MergeCommand(DbContext context, TEntity entity, SqlTransaction transaction = null)
            : this(context, transaction)
        {
            _entityList = new List<TEntity>() { entity };
            _useTVP = false;
        }

        /// <summary>
        /// Merge list of entities into the table and pass values as SqlParameter.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entityList"></param>
        /// <param name="transaction"></param>
        public MergeCommand(DbContext context, List<TEntity> entityList, SqlTransaction transaction = null)
            : this(context, transaction)
        {
            _entityList = entityList;
            _useTVP = false;
        }

        /// <summary>
        /// Merge list of entities into the table and pass values as TVP. Order of selected Source fields must match the order of columns in TVP declaration.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entityList"></param>
        /// <param name="sqlTVPTypeName"></param>
        /// <param name="sqlTVPParameterName"></param>
        /// <param name="transaction"></param>
        public MergeCommand(DbContext context, List<TEntity> entityList
            , string sqlTVPTypeName, string sqlTVPParameterName = "@Table")
            : this(context)
        {
            _entityList = entityList;
            _useTVP = true;
            SqlTVPTypeName = sqlTVPTypeName;
            SqlTVPParameterName = sqlTVPParameterName;
        }



        //public methods
        public virtual int Execute(MergeType mergeType)
        {
            if (_entityList.Count == 0)
            {
                return 0;
            }

            string sqlCommand = ConstructCommand(mergeType);
            SqlParameter[] sqlParameters = _useTVP
                ? ConstructParameterTVP(_entityList)
                : ConstructParameterValues(_entityList);
            
            bool hasOutput = Output.GetSelectedFlat().Count > 0;
            if (hasOutput)
            {
                return ReadOutput(sqlCommand, sqlParameters.ToArray(), _entityList);
            }
            else
            {
                return _context.Database
                    .ExecuteSqlCommand(sqlCommand, sqlParameters.ToArray());
            }
        }

        public virtual Task<int> ExecuteAsync(MergeType mergeType)
        {
            if (_entityList.Count == 0)
            {
                return Task.FromResult(0);
            }

            string sqlCommand = ConstructCommand(mergeType);
            SqlParameter[] sqlParameters = _useTVP
                ? ConstructParameterTVP(_entityList)
                : ConstructParameterValues(_entityList);

            bool hasOutput = Output.GetSelectedFlat().Count > 0;
            if (hasOutput)
            {
                return ReadOutputAsync(sqlCommand, sqlParameters.ToArray(), _entityList);
            }
            else
            {
                return _context.Database.ExecuteSqlCommandAsync(sqlCommand, sqlParameters.ToArray());
            }

        }

        public virtual string ConstructCommand(MergeType mergeType)
        {
            _mergeType = mergeType;

            StringBuilder sqlScript = null;

            //head
            if (_useTVP)
            {
                sqlScript = ConstructHeadTVP(_entityList);
            }
            else
            {
                sqlScript = ConstructHeadValues(_entityList);
            }

            //body
            return ConstructBody(sqlScript);
        }
        

        //parameters
        protected virtual SqlParameter[] ConstructParameterValues(List<TEntity> entities)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            for (int i = 0; i < entities.Count; i++)
            {
                TEntity entity = entities[i];
                List<MappedProperty> entityProperties = Source.GetSelectedFlatWithValues(entity);

                foreach (MappedProperty property in entityProperties)
                {
                    if (property.Value != null)
                    {
                        string paramName = $"@{property.EfMappedName}{i}";
                        var sqlParameter = new SqlParameter(paramName, property.Value);
                        sqlParams.Add(sqlParameter);
                    }
                }
            }

            return sqlParams.ToArray();
        }

        protected virtual SqlParameter[] ConstructParameterTVP(List<TEntity> entities)
        {
            List<string> sourcePropertyNames = Source.GetSelectedPropertyNames();
            DataTable entitiesDataTable = entities.ToDataTable(sourcePropertyNames);

            SqlParameter tableParam = new SqlParameter(SqlTVPParameterName, entitiesDataTable);
            tableParam.SqlDbType = SqlDbType.Structured;
            tableParam.TypeName = SqlTVPTypeName;

            return new SqlParameter[] { tableParam };
        }
                

        //head
        protected virtual StringBuilder ConstructHeadValues(List<TEntity> entities)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("MERGE INTO {0} AS {1} ", TableName, _targetAlias);
            sql.AppendFormat("USING (VALUES ");

            //values
            for (int i = 0; i < entities.Count; i++)
            {
                sql.Append("(");
                sql.Append(i + ",");    //row id to match command outputs to input entities

                TEntity entity = entities[i];
                List<MappedProperty> entityProperties = Source.GetSelectedFlatWithValues(entity);

                for (int p = 0; p < entityProperties.Count; p++)
                {
                    if (entityProperties[p].Value == null)
                    {
                        sql.Append("NULL");
                    }
                    else
                    {
                        string paramName = $"@{entityProperties[p].EfMappedName}{i}";
                        sql.Append(paramName);
                    }

                    bool isLastProperty = p == entityProperties.Count - 1;
                    if (isLastProperty == false)
                    {
                        sql.Append(",");
                    }
                }

                sql.Append(")");
                bool isLastEntity = i == entities.Count - 1;
                if (isLastEntity == false)
                {
                    sql.Append(",");
                }
            }

            //column names
            List<string> columnNameList = Source.GetSelectedFlat()
                .Select(x => x.EfMappedName)
                .ToList();

            columnNameList.Insert(0, SOURCE_ID_COLUMN_NAME);    //row id to match command outputs to input entities
            string columnNamesString = string.Join(",", columnNameList);
            sql.AppendFormat(") AS {0} ({1})", _sourceAlias, columnNamesString);

            return sql;
        }

        protected virtual StringBuilder ConstructHeadTVP(List<TEntity> entities)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("MERGE INTO {0} AS {1} ", TableName, _targetAlias);
            sql.AppendFormat("USING {0} AS {1}", SqlTVPParameterName, _sourceAlias);

            return sql;
        }


        //body
        protected virtual string ConstructBody(StringBuilder sql)
        {
            //on
            ConstructOn(sql);


            //merge update
            bool doUpdate = _mergeType == MergeType.Update
                || _mergeType == MergeType.Upsert;
            if (doUpdate)
            {
                ConstructUpdate(sql);
            }
            if (_mergeType == MergeType.Update)
            {
                ConstructUpdateNotMatched(sql);
            }

            //merge insert
            bool doInsert = _mergeType == MergeType.Insert
                || _mergeType == MergeType.Upsert;
            if (doInsert)
            {
                ConstructInsert(sql);
            }

            //merge delete
            bool doDelete = _mergeType == MergeType.DeleteMatched
                || _mergeType == MergeType.DeleteNotMatched;
            if (doDelete)
            {
                ConstructDelete(sql);
            }

            //output
            ConstructOutput(sql);

            sql.Append(";");
            return sql.ToString();
        }

        protected virtual void ConstructOn(StringBuilder sql)
        {
            List<string> stringParts = Compare.GetSelectedFlat()
                .Select(p => string.Format("[{0}].[{1}]=[{2}].[{1}]", _targetAlias, p.EfMappedName, _sourceAlias)).ToList();

            foreach (Expression item in Compare.Expressions)
            {
                string expressionSql = item.ToMSSqlString(_context);
                stringParts.Add(expressionSql);
            }

            string compare = string.Join(" AND ", stringParts);
            sql.AppendFormat(" ON ({0}) ", compare);
        }

        protected virtual void ConstructUpdate(StringBuilder sql)
        {
            List<string> stringParts = UpdateMatched.GetSelectedFlat()
                .Select(c => string.Format("[{0}].[{1}]=[{2}].[{1}]", _targetAlias, c.EfMappedName, _sourceAlias))
                .ToList();

            foreach (Expression item in UpdateMatched.Expressions)
            {
                string expressionSql = item.ToMSSqlString(_context);
                stringParts.Add(expressionSql);
            }

            if (stringParts.Count > 0)
            {
                string update = string.Join(", ", stringParts);
                sql.AppendFormat("WHEN MATCHED THEN UPDATE SET {0}", update);
            }
        }

        protected virtual void ConstructUpdateNotMatched(StringBuilder sql)
        {
            List<string> stringParts = UpdateNotMatched.GetSelectedFlat()
                .Select(c => string.Format("[{0}].[{1}]=[{2}].[{1}]", _targetAlias, c.EfMappedName, _sourceAlias))
                .ToList();

            foreach (Expression item in UpdateNotMatched.Expressions)
            {
                string expressionSql = item.ToMSSqlString(_context);
                stringParts.Add(expressionSql);
            }

            if(stringParts.Count > 0)
            {
                string update = string.Join(", ", stringParts);
                sql.AppendFormat("WHEN NOT MATCHED THEN UPDATE SET {0}", update);
            }
        }

        protected virtual void ConstructInsert(StringBuilder sql)
        {
            List<MappedProperty> selectedProperties = Insert.GetSelectedFlat();
            List<MappedProperty> allProperties = _mergePropertyUtility.FlattenHierarchy(_entityProperties);

            List<string> columnNames = new List<string>();
            List<string> values = new List<string>();

            foreach (MappedProperty selectedProperty in selectedProperties)
            {
                columnNames.Add(selectedProperty.EfMappedName);
                values.Add(_sourceAlias + "." + selectedProperty.EfMappedName);
            }

            foreach (MappedProperty entityProperty in allProperties)
            {
                if (Insert.Defaults.ContainsKey(entityProperty.EfDefaultName))
                {
                    columnNames.Add(entityProperty.EfMappedName);
                    string value = Insert.Defaults[entityProperty.EfDefaultName];
                    values.Add(value);
                }
            }

            string targetString = string.Join(",", columnNames);
            string sourceString = string.Join(", ", values);

            sql.AppendFormat(" WHEN NOT MATCHED THEN INSERT ({0})", targetString);
            sql.AppendFormat(" VALUES ({0})", sourceString);
        }

        protected virtual void ConstructDelete(StringBuilder sql)
        {
            string not = _mergeType == MergeType.DeleteMatched
                ? ""
                : " NOT";
            sql.AppendFormat($" WHEN{not} MATCHED THEN DELETE");
        }


        //output
        protected virtual void ConstructOutput(StringBuilder sql)
        {
            List<MappedProperty> outputProperties = Output.GetSelectedFlat();
            if (outputProperties.Count == 0)
            {
                return;
            }

            sql.Append(" OUTPUT ");
            sql.Append(_sourceAlias + "." + SOURCE_ID_COLUMN_NAME + ",");

            for (int i = 0; i < outputProperties.Count; i++)
            {
                MappedProperty prop = outputProperties[i];
                string name = $"INSERTED.{prop.EfMappedName}";
                sql.Append(name);

                bool isLast = i == outputProperties.Count - 1;
                if (isLast == false)
                {
                    sql.Append(",");
                }
            }
        }

        protected virtual int ReadOutput(string sql, SqlParameter[] parameters, List<TEntity> entities)
        {
            SqlConnection con = (SqlConnection)_context.Database.Connection;
            using (SqlCommand cmd = InitCommandWithParameters(sql, con, parameters))
            {
                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    return ReadFromDataReader(entities, dr);
                }
            }
        }

        protected virtual async Task<int> ReadOutputAsync(string sql, SqlParameter[] parameters, List<TEntity> entities)
        {
            SqlConnection con = (SqlConnection)_context.Database.Connection;
            using (SqlCommand cmd = InitCommandWithParameters(sql, con, parameters))
            {
                if (con.State != ConnectionState.Open)
                {
                    con.Open();
                }
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    return ReadFromDataReader(entities, dr);
                }
            }
        }

        protected virtual SqlCommand InitCommandWithParameters(string sql, SqlConnection con, SqlParameter[] parameters)
        {
            SqlCommand cmd = _transaction == null
                ? new SqlCommand(sql, con)
                : new SqlCommand(sql, con, _transaction);
            cmd.CommandType = CommandType.Text;

            foreach (SqlParameter param in parameters)
            {
                cmd.Parameters.Add(param);
            }

            return cmd;
        }

        protected virtual int ReadFromDataReader(List<TEntity> entities, SqlDataReader datareader)
        {
            List<MappedProperty> outputProperties = Output.GetSelectedFlat();
            int changes = 0;

            if (datareader.HasRows)
            {
                while (datareader.Read())
                {
                    changes++;

                    int sourceRowId = (int)datareader[SOURCE_ID_COLUMN_NAME];
                    TEntity entity = entities[sourceRowId];

                    foreach (MappedProperty prop in outputProperties)
                    {
                        object value = datareader[prop.EfMappedName];
                        Type propType = Nullable.GetUnderlyingType(prop.PropertyInfo.PropertyType) ?? prop.PropertyInfo.PropertyType;
                        value = value == null
                            ? null
                            : Convert.ChangeType(value, propType);
                        prop.PropertyInfo.SetValue(entity, value);
                    }
                }
            }

            datareader.Close();
            return changes;
        }
    }
}
