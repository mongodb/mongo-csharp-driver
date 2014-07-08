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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Async
{
    public class AsyncDropbox<TId, TMessage>
    {
        // fields
        private readonly object _lock = new object();
        private readonly Dictionary<TId, Queue<TMessage>> _dropbox = new Dictionary<TId, Queue<TMessage>>();
        private readonly Dictionary<TId, TaskCompletionSource<TMessage>> _awaiters = new Dictionary<TId, TaskCompletionSource<TMessage>>();

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
                    Queue<TMessage> queue;
                    if (!_dropbox.TryGetValue(id, out queue))
                    {
                        queue = new Queue<TMessage>(1); // queue length will usually be 1 unless Exhaust is used
                        _dropbox.Add(id, queue);
                    }
                    queue.Enqueue(message);
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
                Queue<TMessage> queue;
                if (_dropbox.TryGetValue(id, out queue))
                {
                    var message = queue.Dequeue();
                    if (queue.Count == 0)
                    {
                        _dropbox.Remove(id);
                    }
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
