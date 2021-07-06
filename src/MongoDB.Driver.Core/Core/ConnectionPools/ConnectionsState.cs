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
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.ConnectionPools
{
    internal class ConnectionsState
    {
        private readonly Dictionary<ObjectId, ServiceState> _serviceStates = new();
        private object _lock = new object();

        public void AddConnectionStateForConnectionIfSupported(ConnectionDescription description)
        {
            if (description != null && description.ServiceId.HasValue)
            {
                AddConnectionState(description.ServiceId.Value);
            }
        }

        public void IncreamentGenerationAndCleanConnections(ObjectId serviceId)
        {
            lock (_lock)
            {
                if (!_serviceStates.ContainsKey(serviceId))
                {
                    // a connection can fail during a handshake, in this case serviceId won't be added to the _serviceStates yet
                    _serviceStates.Add(serviceId, ServiceState.CreateInitial());
                }
                _serviceStates[serviceId].Generation.Increment();
            }
        }

        public void RemoveConnectionStateForConnectionIfSupported(ConnectionDescription description)
        {
            if (description != null && description.ServiceId.HasValue)
            {
                RemoveConnectionState(description.ServiceId.Value);
            }
        }

        public bool TryGetGenerationForConnection(ConnectionDescription description, out int generation)
        {
            if (description != null && description.ServiceId.HasValue && TryGetGeneration(description.ServiceId.Value, out generation))
            {
                return true;
            }
            else
            {
                generation = 0;
                return false;
            }
        }

        // private methods
        private void AddConnectionState(ObjectId serviceId)
        {
            lock (_lock)
            {
                if (!_serviceStates.TryGetValue(serviceId, out var serviceState))
                {
                    _serviceStates.Add(serviceId, ServiceState.CreateInitial());
                }
                else
                {
                    serviceState.ConnectionsCount.Increment();
                }
            }
        }

        private void RemoveConnectionState(ObjectId serviceId)
        {
            lock (_lock)
            {
                if (!_serviceStates.TryGetValue(serviceId, out var serviceState))
                {
                    // should not be reached
                    throw new InvalidOperationException("RemoveConnection has no target.");
                }
                if (serviceState.ConnectionsCount.DecrementAndReturn() == 0)
                {
                    _serviceStates.Remove(serviceId);
                }
            }
        }

        private bool TryGetGeneration(ObjectId objectId, out int generation)
        {
            lock (_lock)
            {
                if (_serviceStates.TryGetValue(objectId, out var serviceState))
                {
                    generation = serviceState.Generation.Value;
                    return true;
                }
                else
                {
                    generation = 0;
                    return false;
                }
            }
        }

        // nested types
        private class ServiceState
        {
            #region static
            public static ServiceState CreateInitial() => new ServiceState(generation: 0, connectionCount: 0);
            #endregion

            private Counter _generationCounter;
            private Counter _connectionCountCounter;

            private ServiceState(int generation, int connectionCount)
            {
                _connectionCountCounter = new Counter(connectionCount);
                _generationCounter = new Counter(generation);
            }

            public Counter ConnectionsCount => _connectionCountCounter;

            public Counter Generation => _generationCounter;
        }

        private class Counter
        {
            private int _value;

            public Counter(int value) => _value = value;

            public int Value => _value;
            public void Increment() => _value++;
            public int DecrementAndReturn() => _value--;
        }
    }
}
