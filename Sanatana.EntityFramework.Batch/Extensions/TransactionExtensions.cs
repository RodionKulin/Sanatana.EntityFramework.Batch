using Sanatana.EntityFramework.Batch.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;

namespace Sanatana.EntityFramework.Batch
{
    public static class TransactionExtensions
    {
        public static TransactionScope CreateNoLockTransaction()
        {
            var options = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadUncommitted
            };
            return new TransactionScope(TransactionScopeOption.Required, options);
        }

        public static List<T> ToListNoLock<T>(this IEnumerable<T> query)
        {
            using (TransactionScope ts = CreateNoLockTransaction())
            {
                return query.ToList();
            }
        }
    }
}
