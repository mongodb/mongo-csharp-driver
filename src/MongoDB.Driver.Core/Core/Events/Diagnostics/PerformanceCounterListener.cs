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
    /// Represents an event listener that records certain events to Windows performance counters.
    /// </summary>
    public class PerformanceCounterListener : IConnectionPoolListener, IConnectionListener
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

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceCounterListener"/> class.
        /// </summary>
        /// <param name="applicationName">The name of the application.</param>
        public PerformanceCounterListener(string applicationName)
        {
            _applicationName = applicationName;
            _packages = new ConcurrentDictionary<string, PerformanceCounterPackage>();
            _appPackage = GetAppPackage();
            _connectionRecorders = new ConcurrentDictionary<ConnectionId, ConnectionPerformanceRecorder>();
            _connectionPoolRecorders = new ConcurrentDictionary<ServerId, ConnectionPoolPerformanceRecorder>();
        }

        // methods
        // Connection Pool
        /// <inheritdoc/>
        public void ConnectionPoolAfterClosing(ConnectionPoolAfterClosingEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryRemove(@event.ServerId, out recorder))
            {
                recorder.Closed();
            }
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterOpening(ConnectionPoolAfterOpeningEvent @event)
        {
            var serverPackage = GetServerPackage(@event.ServerId.EndPoint);
            ConnectionPoolPerformanceRecorder recorder = new ConnectionPoolPerformanceRecorder(@event.ConnectionPoolSettings.MaxConnections, _appPackage, serverPackage);
            if (_connectionPoolRecorders.TryAdd(@event.ServerId, recorder))
            {
                recorder.Opened();
            }
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterAddingAConnection(ConnectionPoolAfterAddingAConnectionEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ConnectionId.ServerId, out recorder))
            {
                recorder.ConnectionAdded();
            }
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterRemovingAConnection(ConnectionPoolAfterRemovingAConnectionEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ConnectionId.ServerId, out recorder))
            {
                recorder.ConnectionRemoved();
            }
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterEnteringWaitQueue(ConnectionPoolAfterEnteringWaitQueueEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ServerId, out recorder))
            {
                recorder.WaitQueueEntered();
            }
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterCheckingOutAConnection(ConnectionPoolAfterCheckingOutAConnectionEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ConnectionId.ServerId, out recorder))
            {
                recorder.ConnectionCheckedOut();
                recorder.WaitQueueExited();
            }
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterCheckingInAConnection(ConnectionPoolAfterCheckingInAConnectionEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ConnectionId.ServerId, out recorder))
            {
                recorder.ConnectionCheckedIn();
            }
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeClosing(ConnectionPoolBeforeClosingEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeOpening(ConnectionPoolBeforeOpeningEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeAddingAConnection(ConnectionPoolBeforeAddingAConnectionEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeRemovingAConnection(ConnectionPoolBeforeRemovingAConnectionEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeEnteringWaitQueue(ConnectionPoolBeforeEnteringWaitQueueEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionPoolErrorEnteringWaitQueue(ConnectionPoolErrorEnteringWaitQueueEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeCheckingOutAConnection(ConnectionPoolBeforeCheckingOutAConnectionEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionPoolErrorCheckingOutAConnection(ConnectionPoolErrorCheckingOutAConnectionEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeCheckingInAConnection(ConnectionPoolBeforeCheckingInAConnectionEvent @event)
        {
        }

        // Connection
        /// <inheritdoc/>
        public void ConnectionAfterClosing(ConnectionAfterClosingEvent @event)
        {
            ConnectionPerformanceRecorder recorder;
            if (_connectionRecorders.TryRemove(@event.ConnectionId, out recorder))
            {
                recorder.Closed();
            }
        }

        /// <inheritdoc/>
        public void ConnectionAfterOpening(ConnectionAfterOpeningEvent @event)
        {
            var serverPackage = GetServerPackage(@event.ConnectionId.ServerId.EndPoint);
            var recorder = new ConnectionPerformanceRecorder(_appPackage, serverPackage);
            if (_connectionRecorders.TryAdd(@event.ConnectionId, recorder))
            {
                recorder.Opened();
            }
        }

        /// <inheritdoc/>
        public void ConnectionAfterReceivingMessage(ConnectionAfterReceivingMessageEvent @event)
        {
            ConnectionPerformanceRecorder recorder;
            if (_connectionRecorders.TryGetValue(@event.ConnectionId, out recorder))
            {
                recorder.MessageReceived(@event.ReceivedMessage.ResponseTo, @event.Length);
            }
        }

        /// <inheritdoc/>
        public void ConnectionAfterSendingMessages(ConnectionAfterSendingMessagesEvent @event)
        {
            ConnectionPerformanceRecorder recorder;
            if (_connectionRecorders.TryGetValue(@event.ConnectionId, out recorder))
            {
                recorder.PacketSent(@event.Messages.Count, @event.Length);
            }
        }

        /// <inheritdoc/>
        public void ConnectionFailed(ConnectionFailedEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionBeforeClosing(ConnectionBeforeClosingEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionBeforeOpening(ConnectionBeforeOpeningEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionBeforeReceivingMessage(ConnectionBeforeReceivingMessageEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionBeforeSendingMessages(ConnectionBeforeSendingMessagesEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionErrorOpening(ConnectionErrorOpeningEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionErrorReceivingMessage(ConnectionErrorReceivingMessageEvent @event)
        {
        }

        /// <inheritdoc/>
        public void ConnectionErrorSendingMessages(ConnectionErrorSendingMessagesEvent @event)
        {
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
