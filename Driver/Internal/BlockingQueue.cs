/* Copyright 2010 10gen Inc.
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

namespace MongoDB.Driver.Internal {
    public class BlockingQueue<T> {
        #region private fields
        private object syncRoot = new object();
        private Queue<T> queue = new Queue<T>();
        #endregion

        #region constructors
        public BlockingQueue() {
        }
        #endregion

        #region public methods
        public T Dequeue(
            TimeSpan timeout
        ) {
            lock (syncRoot) {
                var timeoutAt = DateTime.UtcNow + timeout;
                while (queue.Count == 0) {
                    var timeRemaining = timeoutAt - DateTime.UtcNow;
                    if (timeRemaining > TimeSpan.Zero) {
                        Monitor.Wait(syncRoot, timeRemaining);
                    } else {
                        return default(T);
                    }
                }
                return queue.Dequeue();
            }
        }

        public void Enqueue(
            T item
        ) {
            lock (syncRoot) {
                queue.Enqueue(item);
                Monitor.Pulse(syncRoot);
            }
        }
        #endregion
    }
}
