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
        public void AfterClosing(ConnectionPoolAfterClosingEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryRemove(@event.ServerId, out recorder))
            {
                recorder.Closed();
            }
        }

        public void AfterOpening(ConnectionPoolAfterOpeningEvent @event)
        {
            var serverPackage = GetServerPackage(@event.ServerId.EndPoint);
            ConnectionPoolPerformanceRecorder recorder = new ConnectionPoolPerformanceRecorder(@event.ConnectionPoolSettings.MaxConnections, _appPackage, serverPackage);
            if (_connectionPoolRecorders.TryAdd(@event.ServerId, recorder))
            {
                recorder.Opened();
            }
        }

        public void AfterAddingAConnection(ConnectionPoolAfterAddingAConnectionEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ConnectionId.ServerId, out recorder))
            {
                recorder.ConnectionAdded();
            }
        }

        public void AfterRemovingAConnection(ConnectionPoolAfterRemovingAConnectionEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ConnectionId.ServerId, out recorder))
            {
                recorder.ConnectionRemoved();
            }
        }

        public void AfterEnteringWaitQueue(ConnectionPoolAfterEnteringWaitQueueEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ServerId, out recorder))
            {
                recorder.WaitQueueEntered();
            }
        }

        public void AfterCheckingOutAConnection(ConnectionPoolAfterCheckingOutAConnectionEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ConnectionId.ServerId, out recorder))
            {
                recorder.ConnectionCheckedOut();
                recorder.WaitQueueExited();
            }
        }

        public void AfterCheckingInAConnection(ConnectionPoolAfterCheckingInAConnectionEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ConnectionId.ServerId, out recorder))
            {
                recorder.ConnectionCheckedIn();
            }
        }

        public void BeforeClosing(ConnectionPoolBeforeClosingEvent @event)
        {
        }

        public void BeforeOpening(ConnectionPoolBeforeOpeningEvent @event)
        {
        }

        public void BeforeAddingAConnection(ConnectionPoolBeforeAddingAConnectionEvent @event)
        {
        }

        public void BeforeRemovingAConnection(ConnectionPoolBeforeRemovingAConnectionEvent @event)
        {
        }

        public void BeforeEnteringWaitQueue(ConnectionPoolBeforeEnteringWaitQueueEvent @event)
        {
        }

        public void ErrorEnteringWaitQueue(ConnectionPoolErrorEnteringWaitQueueEvent @event)
        {
        }

        public void BeforeCheckingOutAConnection(ConnectionPoolBeforeCheckingOutAConnectionEvent @event)
        {
        }

        public void ErrorCheckingOutAConnection(ConnectionPoolErrorCheckingOutAConnectionEvent @event)
        {
        }

        public void BeforeCheckingInAConnection(ConnectionPoolBeforeCheckingInAConnectionEvent @event)
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
