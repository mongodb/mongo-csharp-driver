using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Operations
{
    public interface IAsyncCursor<TDocument> : IDisposable
    {
        IEnumerable<TDocument> Current { get; }

        Task<bool> MoveNextAsync();
    }
}
