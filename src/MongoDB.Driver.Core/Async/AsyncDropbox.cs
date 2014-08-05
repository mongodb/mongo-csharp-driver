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
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Async
{
    public class AsyncDropbox<TId, TMessage>
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
                awaiter.TrySetResult(message);
            }
        }

        public Task<TMessage> ReceiveAsync(TId id, TimeSpan timeout, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                TMessage message;
                if (_messages.TryGetValue(id, out message))
                {
                    _messages.Remove(id);
                    return Task.FromResult(message);
                }
                else
                {
                    var awaiter = new TaskCompletionSource<TMessage>().WithTimeout(timeout).WithCancellationToken(cancellationToken);
                    _awaiters.Add(id, awaiter);
                    return awaiter.Task;
                }
            }
        }
    }
}
