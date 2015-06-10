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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events.Diagnostics.PerformanceCounters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Events.Diagnostics
{
    /// <preliminary/>
    /// <summary>
    /// Represents an event subscriber that records certain events to Windows performance counters.
    /// </summary>
    public class PerformanceCounterEventSubscriber : IEventSubscriber
    {
        //static 
        /// <summary>
        /// Installs the performance counters.
        /// </summary>
        public static void InstallPerformanceCounters()
        {
            PerformanceCounterPackage.Install();
        }

        // fields
        private readonly string _applicationName;
        private readonly PerformanceCounterPackage _appPackage;
        private readonly ConcurrentDictionary<string, PerformanceCounterPackage> _packages;
        private readonly ConcurrentDictionary<ConnectionId, ConnectionPerformanceRecorder> _connectionRecorders;
        private readonly ConcurrentDictionary<ServerId, ConnectionPoolPerformanceRecorder> _connectionPoolRecorders;
        private readonly IEventSubscriber _subscriber;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceCounterEventSubscriber"/> class.
        /// </summary>
        /// <param name="applicationName">The name of the application.</param>
        public PerformanceCounterEventSubscriber(string applicationName)
        {
            _applicationName = applicationName;
            _packages = new ConcurrentDictionary<string, PerformanceCounterPackage>();
            _appPackage = GetAppPackage();
            _connectionRecorders = new ConcurrentDictionary<ConnectionId, ConnectionPerformanceRecorder>();
            _connectionPoolRecorders = new ConcurrentDictionary<ServerId, ConnectionPoolPerformanceRecorder>();
            _subscriber = new ReflectionEventSubscriber(this);
        }

        // methods
        /// <inheritdoc />
        public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
        {
            return _subscriber.TryGetEventHandler(out handler);
        }

        /// <summary>
        /// Handles the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Handle(ConnectionPoolClosedEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryRemove(@event.ServerId, out recorder))
            {
                recorder.Closed();
            }
        }

        /// <summary>
        /// Handles the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Handle(ConnectionPoolOpenedEvent @event)
        {
            var serverPackage = GetServerPackage(@event.ServerId.EndPoint);
            ConnectionPoolPerformanceRecorder recorder = new ConnectionPoolPerformanceRecorder(@event.ConnectionPoolSettings.MaxConnections, _appPackage, serverPackage);
            if (_connectionPoolRecorders.TryAdd(@event.ServerId, recorder))
            {
                recorder.Opened();
            }
        }

        /// <summary>
        /// Handles the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Handle(ConnectionPoolAddedConnectionEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ServerId, out recorder))
            {
                recorder.ConnectionAdded();
            }
        }

        /// <summary>
        /// Connections the pool after removing a connection.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Handle(ConnectionPoolRemovedConnectionEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ServerId, out recorder))
            {
                recorder.ConnectionRemoved();
            }
        }

        /// <summary>
        /// Connections the pool after entering wait queue.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Handle(ConnectionPoolCheckingOutConnectionEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ServerId, out recorder))
            {
                recorder.WaitQueueEntered();
            }
        }

        /// <summary>
        /// Handles the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Handle(ConnectionPoolCheckedOutConnectionEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ServerId, out recorder))
            {
                recorder.ConnectionCheckedOut();
                recorder.WaitQueueExited();
            }
        }

        /// <summary>
        /// Handles the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <inheritdoc />
        public void Handle(ConnectionPoolCheckedInConnectionEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ServerId, out recorder))
            {
                recorder.ConnectionCheckedIn();
            }
        }

        /// <summary>
        /// Handles the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Handle(ConnectionClosedEvent @event)
        {
            ConnectionPerformanceRecorder recorder;
            if (_connectionRecorders.TryRemove(@event.ConnectionId, out recorder))
            {
                recorder.Closed();
            }
        }

        /// <summary>
        /// Handles the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Handle(ConnectionOpenedEvent @event)
        {
            var serverPackage = GetServerPackage(@event.ServerId.EndPoint);
            var recorder = new ConnectionPerformanceRecorder(_appPackage, serverPackage);
            if (_connectionRecorders.TryAdd(@event.ConnectionId, recorder))
            {
                recorder.Opened();
            }
        }

        /// <summary>
        /// Handles the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Handle(ConnectionReceivedMessageEvent @event)
        {
            ConnectionPerformanceRecorder recorder;
            if (_connectionRecorders.TryGetValue(@event.ConnectionId, out recorder))
            {
                recorder.MessageReceived(@event.ResponseTo, @event.Length);
            }
        }

        /// <summary>
        /// Handles the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Handle(ConnectionSentMessagesEvent @event)
        {
            ConnectionPerformanceRecorder recorder;
            if (_connectionRecorders.TryGetValue(@event.ConnectionId, out recorder))
            {
                recorder.PacketSent(@event.RequestIds.Count, @event.Length);
            }
        }

        private PerformanceCounterPackage CreatePackage(string instanceName)
        {
            return new PerformanceCounterPackage(instanceName);
        }

        private PerformanceCounterPackage GetAppPackage()
        {
            return _packages.GetOrAdd(_applicationName, CreatePackage);
        }

        private PerformanceCounterPackage GetServerPackage(EndPoint endPoint)
        {
            var server = string.Format("{0}_{1}", _applicationName, Format(endPoint));
            return _packages.GetOrAdd(server, CreatePackage);
        }

        private string Format(EndPoint endPoint)
        {
            var dnsEndPoint = endPoint as DnsEndPoint;
            if (dnsEndPoint != null)
            {
                return dnsEndPoint.Host + ":" + dnsEndPoint.Port.ToString();
            }

            var ipEndPoint = endPoint as IPEndPoint;
            if (ipEndPoint != null)
            {
                return ipEndPoint.Address.ToString() + ":" + ipEndPoint.Port.ToString();
            }

            return endPoint.ToString();
        }
    }
}
