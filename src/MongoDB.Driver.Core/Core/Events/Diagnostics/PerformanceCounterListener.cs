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
    public class PerformanceCounterListener : IConnectionPoolListener, IConnectionListener
    {
        //static 
        public static void Install()
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
        public void ConnectionPoolAfterClosing(ServerId serverId)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryRemove(serverId, out recorder))
            {
                recorder.Closed();
            }
        }

        public void ConnectionPoolAfterOpening(ServerId serverId, ConnectionPoolSettings settings)
        {
            var serverPackage = GetServerPackage(serverId.EndPoint);
            ConnectionPoolPerformanceRecorder recorder = new ConnectionPoolPerformanceRecorder(settings.MaxConnections, _appPackage, serverPackage);
            if (_connectionPoolRecorders.TryAdd(serverId, recorder))
            {
                recorder.Opened();
            }
        }

        public void ConnectionPoolAfterAddingAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(connectionId.ServerId, out recorder))
            {
                recorder.ConnectionAdded();
            }
        }

        public void ConnectionPoolAfterRemovingAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(connectionId.ServerId, out recorder))
            {
                recorder.ConnectionRemoved();
            }
        }

        public void ConnectionPoolAfterEnteringWaitQueue(ServerId serverId, TimeSpan elapsed)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(serverId, out recorder))
            {
                recorder.WaitQueueEntered();
            }
        }

        public void ConnectionPoolAfterCheckingOutAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(connectionId.ServerId, out recorder))
            {
                recorder.ConnectionCheckedOut();
                recorder.WaitQueueExited();
            }
        }

        public void ConnectionPoolAfterCheckingInAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(connectionId.ServerId, out recorder))
            {
                recorder.ConnectionCheckedIn();
            }
        }

        public void ConnectionPoolBeforeClosing(ServerId serverId)
        {
        }

        public void ConnectionPoolBeforeOpening(ServerId serverId, ConnectionPoolSettings settings)
        {
        }

        public void ConnectionPoolBeforeAddingAConnection(ServerId serverId)
        {
        }

        public void ConnectionPoolBeforeRemovingAConnection(ConnectionId connectionId)
        {
        }

        public void ConnectionPoolBeforeEnteringWaitQueue(ServerId serverId)
        {
        }

        public void ConnectionPoolErrorEnteringWaitQueue(ServerId serverId, TimeSpan elapsed, Exception exception)
        {
        }

        public void ConnectionPoolBeforeCheckingOutAConnection(ServerId serverId)
        {
        }

        public void ConnectionPoolErrorCheckingOutAConnection(ServerId serverId, TimeSpan elapsed, Exception ex)
        {
        }

        public void ConnectionPoolBeforeCheckingInAConnection(ConnectionId connectionId)
        {
        }

        // Connection
        public void AfterClosing(ConnectionAfterClosingEvent @event)
        {
            ConnectionPerformanceRecorder recorder;
            if (_connectionRecorders.TryRemove(@event.ConnectionId, out recorder))
            {
                recorder.Closed();
            }
        }

        public void AfterOpening(ConnectionAfterOpeningEvent @event)
        {
            var serverPackage = GetServerPackage(@event.ConnectionId.ServerId.EndPoint);
            var recorder = new ConnectionPerformanceRecorder(_appPackage, serverPackage);
            if (_connectionRecorders.TryAdd(@event.ConnectionId, recorder))
            {
                recorder.Opened();
            }
        }

        public void AfterReceivingMessage<T>(ConnectionAfterReceivingMessageEvent<T> @event)
        {
            ConnectionPerformanceRecorder recorder;
            if (_connectionRecorders.TryGetValue(@event.ConnectionId, out recorder))
            {
                recorder.MessageReceived(@event.ReplyMessage.ResponseTo, @event.Length);
            }
        }

        public void AfterSendingMessages(ConnectionAfterSendingMessagesEvent @event)
        {
            ConnectionPerformanceRecorder recorder;
            if (_connectionRecorders.TryGetValue(@event.ConnectionId, out recorder))
            {
                recorder.PacketSent(@event.Messages.Count, @event.Length);
            }
        }

        public void Failed(ConnectionFailedEvent @event)
        {
        }

        public void BeforeClosing(ConnectionBeforeClosingEvent @event)
        {
        }

        public void BeforeOpening(ConnectionBeforeOpeningEvent @event)
        {
        }

        public void BeforeReceivingMessage(ConnectionBeforeReceivingMessageEvent @event)
        {
        }

        public void BeforeSendingMessages(ConnectionBeforeSendingMessagesEvent @event)
        {
        }

        public void ErrorOpening(ConnectionErrorOpeningEvent @event)
        {
        }
        
        public void ErrorReceivingMessage(ConnectionErrorReceivingMessageEvent @event)
        {
        }

        public void ErrorSendingMessages(ConnectionErrorSendingMessagesEvent @event)
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
