using Sanatana.EntityFramework.Batch.ColumnMapping;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Commands
{
    public class InsertCommand<TEntity>
        where TEntity : class
    {
        //fields
        protected DbContext _context;
        protected MappedPropertyUtility _mappedPropertyUtility;
        protected SqlTransaction _transaction;


        //properties
        public MappingComponent<TEntity> Insert { get; protected set; }
        public MappingComponent<TEntity> Output { get; protected set; }


        //init
        public InsertCommand(DbContext context, SqlTransaction transaction = null)
        {
            _context = context;
            _transaction = transaction;

            Type entityType = typeof(TEntity);
            MappedPropertyUtility mappedPropertyUtility = new MappedPropertyUtility(context, entityType);
            List<MappedProperty> properties = mappedPropertyUtility.GetAllEntityProperties();

            Insert = new MappingComponent<TEntity>(properties, mappedPropertyUtility)
            {
                IncludeGeneratedProperties = IncludeDbGeneratedProperties.ExcludeByDefault
            };
            Output = new MappingComponent<TEntity>(properties, mappedPropertyUtility)
            {
                ExcludeAllByDefault = true,
                IncludeGeneratedProperties = IncludeDbGeneratedProperties.IncludeByDefault
            };
        }


        //methods
        public virtual int Execute(List<TEntity> entities)
        {
            StringBuilder sqlBuilder = ContructInsertManyCommand();
            SqlParameter[] parameters = ConstructParametersAndValues(entities, sqlBuilder);
            string command = sqlBuilder.ToString();

            bool hasOutput = Output.GetSelectedFlat().Count > 0;
            if (hasOutput)
            {
                return ReadOutput(command, parameters, entities);
            }
            else
            {
                return _context.Database.ExecuteSqlCommand(command, parameters);
            }
        }

        public virtual async Task<int> ExecuteAsync(List<TEntity> entities)
        {
            StringBuilder sqlBuilder = ContructInsertManyCommand();
            SqlParameter[] parameters = ConstructParametersAndValues(entities, sqlBuilder);
            string command = sqlBuilder.ToString();

            bool hasOutput = Output.GetSelectedFlat().Count > 0;
            if (hasOutput)
            {
                return await ReadOutputAsync(command, parameters, entities)
                    .ConfigureAwait(false);
            }
            else
            {
                return await _context.Database.ExecuteSqlCommandAsync(command, parameters)
                    .ConfigureAwait(false);
            }
        }

        protected virtual StringBuilder ContructInsertManyCommand()
        {
            List<SqlParameter> parameters = new List<SqlParameter>();
            List<MappedProperty> selectedProperties = Insert.GetSelectedFlat();

            //table name
            string tableName = _context.GetTableName<TEntity>();
            StringBuilder sql = new StringBuilder($"INSERT INTO {tableName}");

            //column names
            List<string> columnNames = selectedProperties.Select(x => $"[{x.EfMappedName}]").ToList();
            string columnNamesJoined = string.Join(",", columnNames);
            sql.Append($" ({columnNamesJoined})");

            //Output
            ConstructOutput(sql);

            //values
            sql.Append(" VALUES ");
           
            return sql;
        }

        protected virtual SqlParameter[] ConstructParametersAndValues(List<TEntity> entities, StringBuilder sql)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            for (int i = 0; i < entities.Count; i++)
            {
                sql.Append("(");

                TEntity entity = entities[i];
                List<MappedProperty> entityProperties = Insert.GetSelectedFlatWithValues(entity);

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

                        var sqlParameter = new SqlParameter(paramName, entityProperties[p].Value);
                        sqlParams.Add(sqlParameter);
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

            return sqlParams.ToArray();
        }



        //Output
        protected virtual void ConstructOutput(StringBuilder sql)
        {
            List<MappedProperty> outputProperties = Output.GetSelectedFlat();
            if (outputProperties.Count == 0)
            {
                return;
            }

            sql.Append(" OUTPUT ");
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
                if(con.State != ConnectionState.Open)
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
            int entityIndex = 0;

            if (datareader.HasRows)
            {
                while (datareader.Read())
                {
                    TEntity entity = entities[entityIndex];
                    entityIndex++;

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
            return entityIndex;
        }
    }
}
