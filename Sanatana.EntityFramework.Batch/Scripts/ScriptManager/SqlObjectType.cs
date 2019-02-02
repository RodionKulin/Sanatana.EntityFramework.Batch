using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Scripts
{
    internal enum SqlObjectType
    { 
        StoredProcedure,
        Table,
        AlterTable,
        Index,
        Function,
        TableType, 
        Unknown 
    }
}
