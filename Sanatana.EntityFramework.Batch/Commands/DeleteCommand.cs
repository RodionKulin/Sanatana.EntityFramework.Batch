using Sanatana.EntityFramework.Batch.Expressions;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Commands
{
    public class DeleteCommand<TEntity>
        where TEntity : class
    {
        //fields
        protected DbContext _context;


        //init
        public DeleteCommand(DbContext context)
        {
            _context = context;
        }


        //methods
        public virtual int Execute(Expression<Func<TEntity, bool>> matchExpression)
        {
            string command = ContructDeleteCommand(matchExpression);
            int changes = _context.Database.ExecuteSqlCommand(command);

            return changes;
        }

        public virtual async Task<int> ExecuteAsync(Expression<Func<TEntity, bool>> matchExpression)
        {
            string command = ContructDeleteCommand(matchExpression);
            int changes = await _context.Database.ExecuteSqlCommandAsync(command).ConfigureAwait(false);

            return changes;
        }

        protected virtual string ContructDeleteCommand(
            Expression<Func<TEntity, bool>> matchExpression)
        {
            string tableName = _context.GetTableName<TEntity>();
            string tableAlias = ExpressionsToMSSql.ALIASES[0];
            string matchSql = matchExpression.ToMSSqlString(_context);

            string command = string.Format("DELETE {0} FROM {1} AS {0} WHERE {2}"
                , tableAlias, tableName, matchSql);

            return command;
        }
    }
}
