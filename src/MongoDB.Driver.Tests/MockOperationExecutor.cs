/* Copyright 2010-2015 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Tests
{
    internal class MockOperationExecutor : IOperationExecutor
    {
        private readonly Queue<object> _calls;
        private readonly Queue<object> _results;

        public MockOperationExecutor()
        {
            _calls = new Queue<object>();
            _results = new Queue<object>();
        }

        public int QueuedCallCount
        {
            get { return _calls.Count; }
        }

        public void EnqueueResult<TResult>(TResult result)
        {
            _results.Enqueue(result);
        }

        public void EnqueueException<TResult>(Exception exception)
        {
            _results.Enqueue(exception);
        }

        public TResult ExecuteReadOperation<TResult>(IReadBinding binding, IReadOperation<TResult> operation, CancellationToken cancellationToken)
        {
            _calls.Enqueue(new ReadCall<TResult>
            {
                Binding = binding,
                Operation = operation,
                CancellationToken = cancellationToken
            });

            if (_results.Count > 0)
            {
                var result = _results.Dequeue();

                var exception = result as Exception;
                if (exception != null)
                {
                    throw exception;
                }

                return (TResult)result;
            }

            return default(TResult);
        }

        public Task<TResult> ExecuteReadOperationAsync<TResult>(IReadBinding binding, IReadOperation<TResult> operation, CancellationToken cancellationToken)
        {
            try
            {
                var result = ExecuteReadOperation<TResult>(binding, operation, cancellationToken);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                var tcs = new TaskCompletionSource<TResult>();
                tcs.TrySetException(ex);
                return tcs.Task;
            }
        }

        public TResult ExecuteWriteOperation<TResult>(IWriteBinding binding, IWriteOperation<TResult> operation, CancellationToken cancellationToken)
        {
            _calls.Enqueue(new WriteCall<TResult>
            {
                Binding = binding,
                Operation = operation,
                CancellationToken = cancellationToken
            });

            if (_results.Count > 0)
            {
                var result = _results.Dequeue();

                var exception = result as Exception;
                if (exception != null)
                {
                    throw exception;
                }

                return (TResult)result;
            }

            return default(TResult);
        }

        public Task<TResult> ExecuteWriteOperationAsync<TResult>(IWriteBinding binding, IWriteOperation<TResult> operation, CancellationToken cancellationToken)
        {
            try
            {
                var result = ExecuteWriteOperation<TResult>(binding, operation, cancellationToken);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                var tcs = new TaskCompletionSource<TResult>();
                tcs.TrySetException(ex);
                return tcs.Task;
            }
        }

        public ReadCall<TResult> GetReadCall<TResult>()
        {
            if (_calls.Count == 0)
            {
                throw new InvalidOperationException("No read operation was executed.");
            }

            var call = _calls.Dequeue();
            var readCall = call as ReadCall<TResult>;
            if (readCall == null)
            {
                throw new InvalidOperationException(string.Format("Expected a call of type {0} but had {1}.", typeof(ReadCall<TResult>), call.GetType()));
            }

            return readCall;
        }

        public WriteCall<TResult> GetWriteCall<TResult>()
        {
            if (_calls.Count == 0)
            {
                throw new InvalidOperationException("No write operation was executed.");
            }

            var call = _calls.Dequeue();
            var writeCall = call as WriteCall<TResult>;
            if (writeCall == null)
            {
                throw new InvalidOperationException(string.Format("Expected a call of type {0} but had {1}.", typeof(WriteCall<TResult>), call.GetType()));
            }

            return writeCall;
        }

        public class ReadCall<TResult>
        {
            public IReadBinding Binding { get; set; }
            public IReadOperation<TResult> Operation { get; set; }
            public CancellationToken CancellationToken { get; set; }
        }

        public class WriteCall<TResult>
        {
            public IWriteBinding Binding { get; set; }
            public IWriteOperation<TResult> Operation { get; set; }
            public CancellationToken CancellationToken { get; set; }
        }
    }
}
