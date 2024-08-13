/*Copyright 2021 - present MongoDB Inc.
 *
* Licensed under the Apache License, Version 2.0 (the "License");
*you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
*Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace MongoDB.Driver.Core.ConnectionPools
{
    internal sealed class ServiceStates
    {
        private readonly Dictionary<ObjectId, ServiceState> _serviceStates = new();
        private readonly object _lock = new();

        public void IncrementConnectionCount(ObjectId? serviceId)
        {
            if (serviceId.HasValue)
            {
                lock (_lock)
                {
                    if (!_serviceStates.TryGetValue(serviceId.Value, out var serviceState))
                    {
                        serviceState = new ServiceState();
                        _serviceStates.Add(serviceId.Value, serviceState);
                    }
                    serviceState.ConnectionCount++;
                }
            }
        }

        public void IncrementGeneration(ObjectId serviceId)
        {
            lock (_lock)
            {
                if (!_serviceStates.TryGetValue(serviceId, out var serviceState))
                {
                    serviceState = new ServiceState();
                    // a connection can fail during a handshake, in this case serviceId won't be added to the _serviceStates yet
                    _serviceStates.Add(serviceId, serviceState);
                }
                serviceState.Generation++;
            }
        }

        public void DecrementConnectionCount(ObjectId? serviceId)
        {
            if (serviceId.HasValue)
            {
                lock (_lock)
                {
                    if (!_serviceStates.TryGetValue(serviceId.Value, out var serviceState))
                    {
                        // should not be reached
                        throw new InvalidOperationException("RemoveServiceState has no target.");
                    }
                    if (--serviceState.ConnectionCount == 0)
                    {
                        _serviceStates.Remove(serviceId.Value);
                    }
                }
            }
        }

        public bool TryGetGeneration(ObjectId? serviceId, out int generation)
        {
            if (serviceId.HasValue)
            {
                lock (_lock)
                {
                    if (_serviceStates.TryGetValue(serviceId.Value, out var serviceState))
                    {
                        generation = serviceState.Generation;
                        return true;
                    }
                }
            }

            generation = 0;
            return false;
        }

        // nested types
        private class ServiceState
        {
            public int ConnectionCount { get; set; }
            public int Generation { get; set; }
        }
    }
}
