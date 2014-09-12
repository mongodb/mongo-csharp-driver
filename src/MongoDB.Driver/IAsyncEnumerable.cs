using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    /// <summary>
    /// An enumerable operating on an asynchronous stream.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    public interface IAsyncEnumerable<out T>
    {
        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        IAsyncEnumerator<T> GetEnumerator();
    }
}
