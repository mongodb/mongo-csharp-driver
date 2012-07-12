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
    /// Maintains a sorted list of connected instances by ping time.
    /// </summary>
    internal class ConnectedInstanceCollection
    {
        // private fields
        private readonly object _lock = new object();
        private List<MongoServerInstance> _instances;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectedInstanceCollection"/> class.
        /// </summary>
        public ConnectedInstanceCollection()
        {
            _instances = new List<MongoServerInstance>();
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
                var index = _instances.FindIndex(x => x.AveragePingTime >= instance.AveragePingTime);
                if (index == -1)
                {
                    _instances.Add(instance);
                }
                else
                {
                    _instances.Insert(index + 1, instance);
                }
                instance.AveragePingTimeChanged += InstanceAveragePingTimeChanged;
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
        /// <returns>A MongoServerInstance.</returns>
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
                instance.AveragePingTimeChanged -= InstanceAveragePingTimeChanged;
                _instances.Remove(instance);
            }
        }

        // private methods
        private void InstanceAveragePingTimeChanged(object sender, EventArgs e)
        {
            lock (_lock)
            {
                _instances.Sort((x, y) => x.AveragePingTime.CompareTo(y.AveragePingTime));
            }
        }
    }
}