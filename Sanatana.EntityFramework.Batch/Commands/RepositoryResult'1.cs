using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Commands
{
    public class RepositoryResult<T>
    {
        public List<T> Data { get; set; }
        public long TotalRows { get; set; }
    }
}
