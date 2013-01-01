/* Copyright 2010-2013 10gen Inc.
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

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Represents a thread-safe queue.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    internal class BlockingQueue<T>
    {
        // private fields
        private readonly object _syncRoot = new object();
        private readonly Queue<T> _queue = new Queue<T>();
        private int _pipelineCount;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BlockingQueue class.
        /// </summary>
        public BlockingQueue()
        {
        }

        // public properties
        /// <summary>
        /// Gets the count of the items either ready or in the pipeline.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_syncRoot)
                {
                    return _queue.Count + _pipelineCount;
                }
            }
        }

        // public methods
        /// <summary>
        /// Dequeues one item from the queue.  Will block until the item is available.
        /// </summary>
        /// <returns>The first item in the queue.</returns>
        public T Dequeue()
        {
            lock (_syncRoot)
            {
                while (_queue.Count == 0)
                {
                    Monitor.Wait(_syncRoot);
                }
                return _queue.Dequeue();
            }
        }

        /// <summary>
        /// Dequeues one item from the queue. Will block waiting for an item if the queue is empty.
        /// </summary>
        /// <param name="timeout">The timeout for waiting for an item to appear in the queue.</param>
        /// <returns>The first item in the queue (null if it timed out).</returns>
        public T Dequeue(TimeSpan timeout)
        {
            lock (_syncRoot)
            {
                var timeoutAt = DateTime.UtcNow + timeout;
                while (_queue.Count == 0)
                {
                    var timeRemaining = timeoutAt - DateTime.UtcNow;
                    if (timeRemaining > TimeSpan.Zero)
                    {
                        Monitor.Wait(_syncRoot, timeRemaining);
                    }
                    else
                    {
                        return default(T);
                    }
                }
                return _queue.Dequeue();
            }
        }

        /// <summary>
        /// Enqueues an item on to the queue.
        /// </summary>
        /// <param name="item">The item to be queued.</param>
        public void Enqueue(T item)
        {
            lock (_syncRoot)
            {
                _queue.Enqueue(item);
                Monitor.PulseAll(_syncRoot);
            }
        }

        /// <summary>
        /// Enqueus the work item to run on a seperate-thread.
        /// </summary>
        /// <param name="itemFunc">The item func.</param>
        public void EnqueuWorkItem(Func<T> itemFunc)
        {
            lock (_syncRoot)
            {
                _pipelineCount++;
            }
            ThreadPool.QueueUserWorkItem(o => RunItemFunc(itemFunc));
        }

        // private methods
        private void RunItemFunc(Func<T> itemFunc)
        {
            Enqueue(itemFunc());
            lock (_syncRoot)
            {
                _pipelineCount--;
            }
        }
    }
}