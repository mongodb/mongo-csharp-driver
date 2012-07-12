/* Copyright 2010-2012 10gen Inc.
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
using System.Collections.ObjectModel;
using System.Threading;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Chooses the server in a round robin of those with the lowest ping times.
    /// </summary>
    internal class ConnectedInstanceCollection
    {
        // private fields
        private readonly object _lock = new object();
        private readonly Timer _timer;
        private List<MongoServerInstance> _instances;

        // public properties
        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _instances.Count;
                }
            }
        }

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectedInstanceCollection"/> class.
        /// </summary>
        public ConnectedInstanceCollection()
        {
            _instances = new List<MongoServerInstance>();
            _timer = new Timer(o => Update(), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        // public methods
        /// <summary>
        /// Adds the specified instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public void Add(MongoServerInstance instance)
        {
            lock (_lock)
            {
                _instances.Add(instance);
            }
        }

        /// <summary>
        /// Indicates if the instance exists in the chooser.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>
        ///   <c>true</c> if [contains] [the specified instance]; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(MongoServerInstance instance)
        {
            lock (_lock)
            {
                return _instances.Contains(instance);
            }
        }

        /// <summary>
        /// Chooses the server instance based on the read preference.
        /// </summary>
        /// <param name="readPreference">The read preference.</param>
        /// <returns></returns>
        public MongoServerInstance ChooseServerInstance(ReadPreference readPreference)
        {
            lock (_lock)
            {
                if (_instances.Count == 0)
                {
                    return null;
                }

                return readPreference.ChooseServerInstance(_instances);
            }
        }

        /// <summary>
        /// Removes the specified instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public void Remove(MongoServerInstance instance)
        {
            lock (_lock)
            {
                _instances.Remove(instance);
            }
        }

        // private methods
        /// <summary>
        /// Updates this instance.
        /// </summary>
        private void Update()
        {
            lock (_lock)
            {
                if (_instances.Count == 0)
                {
                    return;
                }

                _instances.Sort((x, y) => x.AveragePingTime.CompareTo(y.AveragePingTime));
            }
        }
    }
}