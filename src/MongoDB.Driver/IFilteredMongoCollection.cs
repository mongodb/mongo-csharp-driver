using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    /// <summary>
    /// A filtered mongo collection. The filter will be and'ed with all filters.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public interface IFilteredMongoCollection<TDocument> : IMongoCollection<TDocument>
    {
        /// <summary>
        /// Gets the filter.
        /// </summary>
        FilterDefinition<TDocument> Filter { get; }
    }
}
