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
using System.Linq;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Maintains a sorted list of connected instances by ping time.
    /// </summary>
    internal class ConnectedInstanceCollection
    {
        // private fields
        private readonly object _connectedInstancesLock = new object();

        private Dictionary<MongoServerInstance, LinkedListNode<InstanceWithPingTime>> _instanceLookup;
        private LinkedList<InstanceWithPingTime> _instances;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectedInstanceCollection"/> class.
        /// </summary>
        public ConnectedInstanceCollection()
        {
            _instances = new LinkedList<InstanceWithPingTime>();
            _instanceLookup = new Dictionary<MongoServerInstance, LinkedListNode<InstanceWithPingTime>>();
        }

        // public methods
        /// <summary>
        /// Clears all the instances.
        /// </summary>
        public void Clear()
        {
            lock (_connectedInstancesLock)
            {
                _instances.Clear();
                _instanceLookup.Clear();
            }
        }

        /// <summary>
        /// Gets a list of all instances.
        /// </summary>
        /// <returns>The list of all instances.</returns>
        public List<InstanceWithPingTime> GetAllInstances()
        {
            lock (_connectedInstancesLock)
            {
                // note: make copies of InstanceWithPingTime values because they can change after we return
                return _instances
                    .Select(x => new InstanceWithPingTime { Instance = x.Instance, CachedAveragePingTime = x.CachedAveragePingTime })
                    .ToList();
            }
        }

        /// <summary>
        /// Gets the primary instance.
        /// </summary>
        /// <returns>The primary instance (or null if there is none).</returns>
        public MongoServerInstance GetPrimary()
        {
            lock (_connectedInstancesLock)
            {
                return _instances.Select(x => x.Instance).FirstOrDefault(i => i.IsPrimary);
            }
        }

        /// <summary>
        /// Gets a list of primary and secondary instances.
        /// </summary>
        /// <returns>The list of primary and secondary instances.</returns>
        public List<InstanceWithPingTime> GetPrimaryAndSecondaries()
        {
            lock (_connectedInstancesLock)
            {
                // note: make copies of InstanceWithPingTime values because they can change after we return
                return _instances
                    .Where(x => x.Instance.IsPrimary || x.Instance.IsSecondary)
                    .Select(x => new InstanceWithPingTime { Instance = x.Instance, CachedAveragePingTime = x.CachedAveragePingTime })
                    .ToList();
            }
        }

        /// <summary>
        /// Gets a list of secondaries.
        /// </summary>
        /// <returns>The list of secondaries.</returns>
        public List<InstanceWithPingTime> GetSecondaries()
        {
            lock (_connectedInstancesLock)
            {
                // note: make copies of InstanceWithPingTime values because they can change after we return
                return _instances
                    .Where(x => x.Instance.IsSecondary)
                    .Select(x => new InstanceWithPingTime { Instance = x.Instance, CachedAveragePingTime = x.CachedAveragePingTime })
                    .ToList();
            }
        }

        /// <summary>
        /// Ensures that the instance is in the collection.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public void EnsureContains(MongoServerInstance instance)
        {
            lock (_connectedInstancesLock)
            {
                if (_instanceLookup.ContainsKey(instance))
                {
                    return;
                }

                var node = new LinkedListNode<InstanceWithPingTime>(new InstanceWithPingTime
                {
                    Instance = instance,
                    CachedAveragePingTime = instance.AveragePingTime
                });

                if (_instances.Count == 0 || _instances.First.Value.CachedAveragePingTime > node.Value.CachedAveragePingTime)
                {
                    _instances.AddFirst(node);
                }
                else
                {
                    var current = _instances.First;

                    while (current.Next != null && node.Value.CachedAveragePingTime > current.Next.Value.CachedAveragePingTime)
                    {
                        current = current.Next;
                    }

                    _instances.AddAfter(current, node);
                }

                _instanceLookup.Add(instance, node);
                instance.AveragePingTimeChanged += InstanceAveragePingTimeChanged;
            }
        }

        /// <summary>
        /// Removes the specified instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public void Remove(MongoServerInstance instance)
        {
            lock (_connectedInstancesLock)
            {
                LinkedListNode<InstanceWithPingTime> node;
                if (!_instanceLookup.TryGetValue(instance, out node))
                {
                    return;
                }

                instance.AveragePingTimeChanged -= InstanceAveragePingTimeChanged;
                _instanceLookup.Remove(instance);
                _instances.Remove(node);
            }
        }

        // private methods
        private void InstanceAveragePingTimeChanged(object sender, EventArgs e)
        {
            var instance = (MongoServerInstance)sender;
            lock (_connectedInstancesLock)
            {
                LinkedListNode<InstanceWithPingTime> node;
                if (!_instanceLookup.TryGetValue(instance, out node))
                {
                    instance.AveragePingTimeChanged -= InstanceAveragePingTimeChanged;
                    return;
                }

                var cachedAveragePingTime = node.Value.CachedAveragePingTime;
                var newPingTime = instance.AveragePingTime;
                node.Value.CachedAveragePingTime = instance.AveragePingTime;
                if (newPingTime < cachedAveragePingTime)
                {
                    var current = node.Previous;

                    if (current == null || current.Value.CachedAveragePingTime < newPingTime)
                    {
                        return;
                    }

                    _instances.Remove(node);

                    while (current.Previous != null && newPingTime < current.Previous.Value.CachedAveragePingTime)
                    {
                        current = current.Previous;
                    }

                    _instances.AddBefore(current, node);
                }
                else if (newPingTime > cachedAveragePingTime)
                {
                    var current = node.Next;

                    if (current == null || current.Value.CachedAveragePingTime > newPingTime)
                    {
                        return;
                    }

                    _instances.Remove(node);

                    while (current.Next != null && newPingTime > current.Next.Value.CachedAveragePingTime)
                    {
                        current = current.Next;
                    }

                    _instances.AddAfter(current, node);
                }
            }
        }

        // When dealing with an always sorted linked list, we need to maintain a cached version of the ping time 
        // to compare against because a MongoServerInstance's could change on it's own making the sorting of the list incorrect.
        internal class InstanceWithPingTime
        {
            public MongoServerInstance Instance;
            public TimeSpan CachedAveragePingTime;
        }
    }
}