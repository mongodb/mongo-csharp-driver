using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    /// <summary>
    /// An asynchronous enumerator.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    public interface IAsyncEnumerator<out T> : IDisposable
    {
        /// <summary>
        /// Gets the current item.
        /// </summary>
        T Current { get; }

        /// <summary>
        /// Tries to moves to the next item.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<bool> MoveNextAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}
