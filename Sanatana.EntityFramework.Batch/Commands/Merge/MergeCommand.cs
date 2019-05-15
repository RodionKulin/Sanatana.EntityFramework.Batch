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
        public CommandArgs<TEntity> Source { get; protected set; }
        /// <summary>
        /// List of columns used to match Target table rows to Source rows.
        /// All properties are excluded by default.
        /// Parameter is required for all merge types except Insert. If not specified will insert all rows into Target table. 
        /// </summary>
        public MergeCompareArgs<TEntity> Compare { get; protected set; }
        /// <summary>
        /// Used if Update or Upsert type of merge is executed.
        /// List of columns to update on Target table for rows that did match Source rows.
        /// All properties are included by default.
        /// </summary>
        public MergeUpdateArgs<TEntity> UpdateMatched { get; protected set; }
        /// <summary>
        /// Used if Update type of merge is executed.
        /// List of columns to update on Target table for rows that did not match Source rows.
        /// All properties are excluded by default.
        /// </summary>
        public MergeUpdateArgs<TEntity> UpdateNotMatched { get; protected set; }
        /// <summary>
        /// Used if Insert or Upsert type of merge is executed.
        /// List of columns to insert.
        /// Database generated properties are excluded by default.
        /// All other properties are included by default.
        /// </summary>
        public MergeInsertArgs<TEntity> Insert { get; protected set; }
        /// <summary>
        /// List of properties to return for inserted rows. 
        /// Include properties that are generated on database side, like auto increment field.
        /// Returned values will be set to provided entities properties.
        /// Database generated or computed properties are included by default.
        /// </summary>
        public CommandArgs<TEntity> Output { get; protected set; }



        //init
        private MergeCommand(DbContext context, SqlTransaction transaction = null)
        {
            _context = context;
            _transaction = transaction;
            TableName = _context.GetTableName<TEntity>();

            Type entityType = typeof(TEntity);
            _mergePropertyUtility = new MappedPropertyUtility(context, entityType);
            _entityProperties = _mergePropertyUtility.GetAllEntityProperties();

            Source = new CommandArgs<TEntity>(_entityProperties, _mergePropertyUtility)
            {
                ExcludeAllByDefault = false,
                ExcludeDbGeneratedByDefault = ExcludeOptions.NotSet
            };
            Compare = new MergeCompareArgs<TEntity>(_entityProperties, _mergePropertyUtility)
            {
                ExcludeAllByDefault = true,
                ExcludeDbGeneratedByDefault = ExcludeOptions.NotSet
            };
            UpdateMatched = new MergeUpdateArgs<TEntity>(_entityProperties, _mergePropertyUtility)
            {
                ExcludeAllByDefault = false,
                ExcludeDbGeneratedByDefault = ExcludeOptions.NotSet
            };
            UpdateNotMatched = new MergeUpdateArgs<TEntity>(_entityProperties, _mergePropertyUtility)
            {
                ExcludeAllByDefault = true,
                ExcludeDbGeneratedByDefault = ExcludeOptions.NotSet
            };
            Insert = new MergeInsertArgs<TEntity>(_entityProperties, _mergePropertyUtility)
            {
                ExcludeAllByDefault = false,
                ExcludeDbGeneratedByDefault = ExcludeOptions.Exclude
            };
            Output = new CommandArgs<TEntity>(_entityProperties, _mergePropertyUtility)
            {
                ExcludeAllByDefault = true,
                ExcludeDbGeneratedByDefault = ExcludeOptions.Include
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
            _mergeType = mergeType;
            if (_entityList.Count == 0)
            {
                return 0;
            }

            List<TEntity>[] entityBatches = GetEntityBatches(_entityList);
            if (_transaction != null)
            {
                return ReadOutput(entityBatches, _transaction);
            }

            int res = 0;
            using (DbContextTransaction batchTransaction =
                _context.Database.BeginTransaction())
            {
                SqlTransaction sqlTransaction = (SqlTransaction)batchTransaction.UnderlyingTransaction;
                res += ReadOutput(entityBatches, sqlTransaction);
                batchTransaction.Commit();
            }
            return res;
        }

        public virtual async Task<int> ExecuteAsync(MergeType mergeType)
        {
            _mergeType = mergeType;
            if (_entityList.Count == 0)
            {
                return 0;
            }

            List<TEntity>[] entityBatches = GetEntityBatches(_entityList);
            if (_transaction != null)
            {
                return await ReadOutputAsync(entityBatches, _transaction)
                    .ConfigureAwait(false);
            }

            int res = 0;
            using (DbContextTransaction batchTransaction = _context.Database.BeginTransaction())
            {
                SqlTransaction sqlTransaction = (SqlTransaction)batchTransaction.UnderlyingTransaction;
                res = await ReadOutputAsync(entityBatches, sqlTransaction).ConfigureAwait(false);
                batchTransaction.Commit();
            }
            return res;
        }



        //limit number of entities in command
        protected virtual List<TEntity>[] GetEntityBatches(List<TEntity> entities)
        {
            int paramsPerEntity = Source.GetSelectedFlat().Count;
            if (paramsPerEntity == 0 || _useTVP)
            {
                return new List<TEntity>[] { entities };
            }

            int maxParams = EntityFrameworkConstants.MAX_NUMBER_OF_SQL_COMMAND_PARAMETERS;
            if (paramsPerEntity > maxParams)
            {
                throw new NotSupportedException($"Single entity can not have more than {maxParams} sql parameters. Consider using TVP version of Merge.");
            }

            int maxEntitiesInBatch = maxParams / paramsPerEntity;
            int requestCount = (int)Math.Ceiling((decimal)entities.Count / maxEntitiesInBatch);

            var results = new List<TEntity>[requestCount];
            for (int request = 0; request < requestCount; request++)
            {
                int skip = maxEntitiesInBatch * request;
                List<TEntity> requestEntities = entities
                    .Skip(skip)
                    .Take(maxEntitiesInBatch)
                    .ToList();
                results[request] = requestEntities;
            }

            return results;
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


        //construct sql command text
        public virtual string ConstructCommand(MergeType mergeType, List<TEntity> entities)
        {
            _mergeType = mergeType;
            StringBuilder sql = _useTVP
                ? ConstructHeadTVP(entities)
                : ConstructHeadValues(entities);

            //on
            if (_mergeType != MergeType.Insert)
            {
                ConstructOn(sql);
            }
            else
            {
                sql.AppendFormat(" ON 1 = 0 ");
            }

            //merge update
            bool doUpdate = _mergeType == MergeType.Update
                || _mergeType == MergeType.Upsert;
            if (doUpdate)
            {
                ConstructUpdateMatched(sql);
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
        
        protected virtual string ConstructBody(StringBuilder sql)
        {
            //on
            ConstructOn(sql);


            //merge update
            bool doUpdate = _mergeType == MergeType.Update
                || _mergeType == MergeType.Upsert;
            if (doUpdate)
            {
                ConstructUpdateMatched(sql);
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

            if(stringParts.Count == 0 && _mergeType == MergeType.Insert)
            {
                stringParts.Add("0=1");
            }
            if (stringParts.Count == 0 && _mergeType != MergeType.Insert)
            {
                throw new ArgumentNullException($"{nameof(Compare)} expression must be specified if it is not an Insert command.");
            }

            string compare = string.Join(" AND ", stringParts);
            sql.AppendFormat(" ON ({0}) ", compare);
        }

        protected virtual void ConstructUpdateMatched(StringBuilder sql)
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

        protected virtual int ReadOutput(List<TEntity>[] entityBatches, SqlTransaction transaction)
        {
            int res = 0;
            foreach (List<TEntity> entitiesBatch in entityBatches)
            {
                string sql = ConstructCommand(_mergeType, entitiesBatch);
                SqlParameter[] parameters = _useTVP
                    ? ConstructParameterTVP(entitiesBatch)
                    : ConstructParameterValues(entitiesBatch);

                bool hasOutput = Output.GetSelectedFlat().Count > 0;
                if (!hasOutput)
                {
                    res += _context.Database.ExecuteSqlCommand(sql, parameters);
                    continue;
                }

                using (SqlCommand cmd = InitCommandWithParameters(sql, parameters, transaction))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    res += ReadFromDataReader(entitiesBatch, dr);
                }
            }
            return res;
        }

        protected virtual async Task<int> ReadOutputAsync(List<TEntity>[] entityBatches, SqlTransaction transaction)
        {
            int res = 0;
            foreach (List<TEntity> entitiesBatch in entityBatches)
            {
                string sql = ConstructCommand(_mergeType, entitiesBatch);
                SqlParameter[] parameters = _useTVP
                    ? ConstructParameterTVP(entitiesBatch)
                    : ConstructParameterValues(entitiesBatch);

                bool hasOutput = Output.GetSelectedFlat().Count > 0;
                if (!hasOutput)
                {
                    res += await _context.Database.ExecuteSqlCommandAsync(sql, parameters)
                        .ConfigureAwait(false);
                    continue;
                }

                using (SqlCommand cmd = InitCommandWithParameters(sql, parameters, transaction))
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync()
                    .ConfigureAwait(false))
                {
                    res += ReadFromDataReader(entitiesBatch, dr);
                }
            }

            return res;
        }

        protected virtual SqlCommand InitCommandWithParameters(string sql,
            SqlParameter[] parameters, SqlTransaction transaction)
        {
            //DbContext will dispose the connection
            SqlConnection con = (SqlConnection)_context.Database.Connection;
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }

            SqlCommand cmd = transaction == null
                ? new SqlCommand(sql, con)
                : new SqlCommand(sql, con, transaction);
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
                        Type propType = Nullable.GetUnderlyingType(prop.PropertyInfo.PropertyType) 
                            ?? prop.PropertyInfo.PropertyType;
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
