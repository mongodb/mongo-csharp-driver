using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    internal class AsyncCursorAsyncEnumerable<TDocument> : IAsyncEnumerable<TDocument>
    {
        private readonly Func<Task<IAsyncCursor<TDocument>>> _executor;
        private readonly long? _limit;

        public AsyncCursorAsyncEnumerable(Func<Task<IAsyncCursor<TDocument>>> executor, long? limit)
        {
            _executor = Ensure.IsNotNull(executor, "executor");
            _limit = limit;
        }

        public IAsyncEnumerator<TDocument> GetEnumerator()
        {
            return new Enumerator(_executor, _limit);
        }

        private class Enumerator : IAsyncEnumerator<TDocument>
        {
            private readonly Func<Task<IAsyncCursor<TDocument>>> _executor;
            private IAsyncEnumerator<TDocument> _enumerator;
            private readonly long? _limit;

            public Enumerator(Func<Task<IAsyncCursor<TDocument>>> executor, long? limit)
            {
                _executor = executor;
                _limit = limit;
            }

            public TDocument Current
            {
                get 
                {
                    if(_enumerator == null)
                    {
                        throw new InvalidOperationException("Enumeration has not started. Call MoveNextAsync.");
                    }

                    return _enumerator.Current; 
                }
            }

            public async Task<bool> MoveNextAsync(TimeSpan? timeout, CancellationToken cancellationToken)
            {
                if(_enumerator == null)
                {
                    var cursor = await _executor();
                    _enumerator = new AsyncCursorEnumerator<TDocument>(cursor, _limit);
                }

                return await _enumerator.MoveNextAsync();
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }

    }
}
