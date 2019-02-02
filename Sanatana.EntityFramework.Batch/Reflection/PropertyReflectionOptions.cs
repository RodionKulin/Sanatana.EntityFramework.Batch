using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Reflection
{
    [Flags]
    public enum PropertyReflectionOptions : int
    {
        /// <summary>
        /// Take all.
        /// </summary>
        All = 0,

        /// <summary>
        /// Ignores indexer properties.
        /// </summary>
        IgnoreIndexer = 1,

        /// <summary>
        /// Ignores all other IEnumerable properties
        /// except strings.
        /// </summary>
        IgnoreEnumerable = 2
    }
}
