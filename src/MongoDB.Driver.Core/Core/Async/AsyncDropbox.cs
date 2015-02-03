/* Copyright 2013-2014 MongoDB Inc.
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Async
{
    internal class AsyncDropbox<TId, TMessage>
    {
        // fields
        private readonly object _lock = new object();
        private readonly Dictionary<TId, TMessage> _messages = new Dictionary<TId, TMessage>();
        private readonly Dictionary<TId, TaskCompletionSource<TMessage>> _awaiters = new Dictionary<TId, TaskCompletionSource<TMessage>>();

        // properties
        public int AwaiterCount
        {
            get
            {
                lock (_lock)
                {
                    return _awaiters.Count;
                }
            }
        }

        public int MessageCount
        {
            get
            {
                lock (_lock)
                {
                    return _messages.Count;
                }
            }
        }

        // methods
        public IEnumerable<TaskCompletionSource<TMessage>> RemoveAllAwaiters()
        {
            lock (_lock)
            {
                var awaiters = _awaiters.Values.ToList();
                _awaiters.Clear();
                return awaiters;
            }
        }

        public void Post(TId id, TMessage message)
        {
            TaskCompletionSource<TMessage> awaiter = null;
            lock (_lock)
            {
                if (_awaiters.TryGetValue(id, out awaiter))
                {
                    _awaiters.Remove(id);
                }
                else
                {
                    _messages.Add(id, message);
                }
            }

            if (awaiter != null)
            {
                if (!awaiter.TrySetResult(message))
                {
                    var disposable = message as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        public async Task<TMessage> ReceiveAsync(TId id, CancellationToken cancellationToken)
        {
            TaskCompletionSource<TMessage> awaiter;
            lock (_lock)
            {
                TMessage message;
                if (_messages.TryGetValue(id, out message))
                {
                    _messages.Remove(id);
                    return message;
                }
                else
                {
                    awaiter = new TaskCompletionSource<TMessage>();
                    _awaiters.Add(id, awaiter);
                }
            }

            using (cancellationToken.Register(() => awaiter.TrySetCanceled(), useSynchronizationContext: false))
            {
                return await awaiter.Task.ConfigureAwait(false);
            }
        }
    }
}
