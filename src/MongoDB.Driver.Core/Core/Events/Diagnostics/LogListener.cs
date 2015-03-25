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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Events.Diagnostics
{
    /// <preliminary/>
    /// <summary>
    /// Represents a log listener that writes log messages to a TextWriter.
    /// </summary>
    public class LogListener : IClusterListener, IServerListener, IConnectionPoolListener, IConnectionListener
    {
        private readonly LogEnricher _enricher;
        private readonly LogLevel _level;
        private readonly TextWriter _writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogListener"/> class.
        /// </summary>
        /// <param name="writer">The text writer.</param>
        /// <param name="level">The log level.</param>
        /// <param name="enricher">The enricher.</param>
        public LogListener(TextWriter writer, LogLevel level = LogLevel.Info, LogEnricher enricher = null)
        {
            _writer = Ensure.IsNotNull(writer, "writer");
            _level = level;
            _enricher = enricher ?? new LogEnricher();
        }

        // Clusters
        /// <inheritdoc/>
        public void ClusterBeforeClosing(ClusterBeforeClosingEvent @event)
        {
            Log(LogLevel.Debug, "{0}: closing.", Label(@event.ClusterId));
        }

        /// <inheritdoc/>
        public void ClusterAfterClosing(ClusterAfterClosingEvent @event)
        {
            Log(LogLevel.Info, "{0}: closed in {1}ms.", Label(@event.ClusterId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        /// <inheritdoc/>
        public void ClusterBeforeOpening(ClusterBeforeOpeningEvent @event)
        {
            Log(LogLevel.Debug, "{0}: opening.", Label(@event.ClusterId));
        }

        /// <inheritdoc/>
        public void ClusterAfterOpening(ClusterAfterOpeningEvent @event)
        {
            Log(LogLevel.Info, "{0}: opened in {1}ms.", Label(@event.ClusterId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        /// <inheritdoc/>
        public void ClusterBeforeAddingServer(ClusterBeforeAddingServerEvent @event)
        {
            Log(LogLevel.Debug, "{0}: adding server at endpoint {1}.", Label(@event.ClusterId), Format(@event.EndPoint));
        }

        /// <inheritdoc/>
        public void ClusterAfterAddingServer(ClusterAfterAddingServerEvent @event)
        {
            Log(LogLevel.Info, "{0}: added server {1} in {2}ms.", Label(@event.ServerId.ClusterId), Format(@event.ServerId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        /// <inheritdoc/>
        public void ClusterBeforeRemovingServer(ClusterBeforeRemovingServerEvent @event)
        {
            Log(LogLevel.Debug, "{0}: removing server {1}. Reason: {2}", Label(@event.ServerId.ClusterId), Format(@event.ServerId), @event.Reason);
        }

        /// <inheritdoc/>
        public void ClusterAfterRemovingServer(ClusterAfterRemovingServerEvent @event)
        {
            Log(LogLevel.Info, "{0}: removed server {1} in {2}ms. Reason: {3}", Label(@event.ServerId.ClusterId), Format(@event.ServerId), @event.Elapsed.TotalMilliseconds.ToString(), @event.Reason);
        }

        /// <inheritdoc/>
        public void ClusterAfterDescriptionChanged(ClusterAfterDescriptionChangedEvent @event)
        {
            Log(LogLevel.Info, "{0}: {1}", Label(@event.OldDescription.ClusterId), @event.NewDescription);
        }

        // Servers
        /// <inheritdoc/>
        public void ServerBeforeClosing(ServerBeforeClosingEvent @event)
        {
            Log(LogLevel.Debug, "{0}: closing.", Label(@event.ServerId));
        }

        /// <inheritdoc/>
        public void ServerAfterClosing(ServerAfterClosingEvent @event)
        {
            Log(LogLevel.Info, "{0}: closed in {1}ms.", Label(@event.ServerId), @event.Elapsed.TotalMilliseconds);
        }

        /// <inheritdoc/>
        public void ServerBeforeOpening(ServerBeforeOpeningEvent @event)
        {
            Log(LogLevel.Debug, "{0}: opening.", Label(@event.ServerId));
        }

        /// <inheritdoc/>
        public void ServerAfterOpening(ServerAfterOpeningEvent @event)
        {
            Log(LogLevel.Info, "{0}: opened in {1}ms.", Label(@event.ServerId), @event.Elapsed.TotalMilliseconds);
        }

        /// <inheritdoc/>
        public void ServerBeforeHeartbeating(ServerBeforeHeartbeatingEvent @event)
        {
            Log(LogLevel.Debug, "{0}: sending heartbeat.", Label(@event.ConnectionId));
        }

        /// <inheritdoc/>
        public void ServerAfterHeartbeating(ServerAfterHeartbeatingEvent @event)
        {
            Log(LogLevel.Info, "{0}: sent heartbeat in {1}ms.", Label(@event.ConnectionId), @event.Elapsed.TotalMilliseconds);
        }

        /// <inheritdoc/>
        public void ServerErrorHeartbeating(ServerErrorHeartbeatingEvent @event)
        {
            Log(LogLevel.Error, "{0}: error sending heartbeat. Exception: {1}", Label(@event.ConnectionId), @event.Exception);
        }

        /// <inheritdoc/>
        public void ServerAfterDescriptionChanged(ServerAfterDescriptionChangedEvent @event)
        {
            Log(LogLevel.Info, "{0}: {1}", Label(@event.OldDescription.ServerId), @event.NewDescription);
        }

        // Connection Pools
        /// <inheritdoc/>
        public void ConnectionPoolBeforeClosing(ConnectionPoolBeforeClosingEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: closing.", Label(@event.ServerId));
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterClosing(ConnectionPoolAfterClosingEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: closed.", Label(@event.ServerId));
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeOpening(ConnectionPoolBeforeOpeningEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: opening.", Label(@event.ServerId));
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterOpening(ConnectionPoolAfterOpeningEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: opened.", Label(@event.ServerId));
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeAddingAConnection(ConnectionPoolBeforeAddingAConnectionEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: adding connection.", Label(@event.ServerId));
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterAddingAConnection(ConnectionPoolAfterAddingAConnectionEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: added connection {1} in {2}ms.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeRemovingAConnection(@ConnectionPoolBeforeRemovingAConnectionEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: removing connection {1}.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId));
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterRemovingAConnection(ConnectionPoolAfterRemovingAConnectionEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: removed connection {1} in {2}ms.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId), @event.Elapsed.TotalMilliseconds);
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeEnteringWaitQueue(ConnectionPoolBeforeEnteringWaitQueueEvent @event)
        {
            // not worth logging
        }

        /// <inheritdoc/>
        public void ConnectionPoolErrorEnteringWaitQueue(ConnectionPoolErrorEnteringWaitQueueEvent @event)
        {
            // not worth logging
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterEnteringWaitQueue(ConnectionPoolAfterEnteringWaitQueueEvent @event)
        {
            // not worth logging
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeCheckingOutAConnection(ConnectionPoolBeforeCheckingOutAConnectionEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: checking out a connection.", Label(@event.ServerId));
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterCheckingOutAConnection(ConnectionPoolAfterCheckingOutAConnectionEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: checked out connection {1} in {2}ms.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        /// <inheritdoc/>
        public void ConnectionPoolErrorCheckingOutAConnection(ConnectionPoolErrorCheckingOutAConnectionEvent @event)
        {
            Log(LogLevel.Error, "{0}-pool: error checking out a connection. Exception: {1}", Label(@event.ServerId), @event.Exception);
        }

        /// <inheritdoc/>
        public void ConnectionPoolBeforeCheckingInAConnection(ConnectionPoolBeforeCheckingInAConnectionEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: checking in connection {1}.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId));
        }

        /// <inheritdoc/>
        public void ConnectionPoolAfterCheckingInAConnection(ConnectionPoolAfterCheckingInAConnectionEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: checked in connection {1} in {2}ms.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        // Connections
        /// <inheritdoc/>
        public void ConnectionFailed(ConnectionFailedEvent @event)
        {
            Log(LogLevel.Error, "{0}: failed. Exception: {1}", Label(@event.ConnectionId), @event.Exception);
        }

        /// <inheritdoc/>
        public void ConnectionBeforeClosing(ConnectionBeforeClosingEvent @event)
        {
            Log(LogLevel.Debug, "{0}: closing.", Label(@event.ConnectionId));
        }

        /// <inheritdoc/>
        public void ConnectionAfterClosing(ConnectionAfterClosingEvent @event)
        {
            Log(LogLevel.Info, "{0}: closed.", Label(@event.ConnectionId));
        }

        /// <inheritdoc/>
        public void ConnectionBeforeOpening(ConnectionBeforeOpeningEvent @event)
        {
            Log(LogLevel.Debug, "{0}: opening.", Label(@event.ConnectionId));
        }

        /// <inheritdoc/>
        public void ConnectionAfterOpening(ConnectionAfterOpeningEvent @event)
        {
            Log(LogLevel.Info, "{0}: opened in {1}ms.", Label(@event.ConnectionId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        /// <inheritdoc/>
        public void ConnectionErrorOpening(ConnectionErrorOpeningEvent @event)
        {
            Log(LogLevel.Error, "{0}: unable to open. Exception: {1}", Label(@event.ConnectionId), @event.Exception);
        }

        /// <inheritdoc/>
        public void ConnectionBeforeReceivingMessage(ConnectionBeforeReceivingMessageEvent @event)
        {
            Log(LogLevel.Debug, "{0}: receiving message in response to {1}.", Label(@event.ConnectionId), @event.ResponseTo.ToString());
        }

        /// <inheritdoc/>
        public void ConnectionAfterReceivingMessage(ConnectionAfterReceivingMessageEvent @event)
        {
            Log(LogLevel.Info, "{0}: received message in response to {1} of length {2} bytes in {3}ms.", Label(@event.ConnectionId), @event.ReceivedMessage.ResponseTo.ToString(), @event.Length.ToString(), @event.Elapsed.TotalMilliseconds.ToString());
        }

        /// <inheritdoc/>
        public void ConnectionErrorReceivingMessage(ConnectionErrorReceivingMessageEvent @event)
        {
            Log(LogLevel.Error, "{0}: error receiving message in response to {1}. Exception: {2}.", Label(@event.ConnectionId), @event.ResponseTo.ToString(), @event.Exception);
        }

        /// <inheritdoc/>
        public void ConnectionBeforeSendingMessages(ConnectionBeforeSendingMessagesEvent @event)
        {
            Log(LogLevel.Debug, "{0}: sending messages [{1}].", Label(@event.ConnectionId), string.Join(",", @event.Messages.Select(x => x.RequestId)));
        }

        /// <inheritdoc/>
        public void ConnectionAfterSendingMessages(ConnectionAfterSendingMessagesEvent @event)
        {
            Log(LogLevel.Info, "{0}: sent messages [{1}] of length {2} bytes in {3}ms.", Label(@event.ConnectionId), string.Join(",", @event.Messages.Select(x => x.RequestId)), @event.Length.ToString(), @event.Elapsed.TotalMilliseconds.ToString());
        }

        /// <inheritdoc/>
        public void ConnectionErrorSendingMessages(ConnectionErrorSendingMessagesEvent @event)
        {
            Log(LogLevel.Error, "{0}: error sending messages [{1}]. Exception: {2}", Label(@event.ConnectionId), string.Join(",", @event.Messages.Select(x => x.RequestId)), @event.Exception);
        }

        private string Label(ConnectionId id)
        {
            var format = "{0}:{1}:{2}";
            string connId;
            if (id.ServerValue.HasValue)
            {
                connId = id.LocalValue.ToString() + "-" + id.ServerValue.Value.ToString();
            }
            else
            {
                connId = id.LocalValue.ToString();
            }

            return string.Format(format, id.ServerId.ClusterId.Value.ToString(), Format(id.ServerId.EndPoint), connId);
        }

        private string Label(ServerId serverId)
        {
            return string.Concat(
                serverId.ClusterId.Value.ToString(),
                ":",
                Format(serverId.EndPoint));
        }

        private string Label(ClusterId clusterId)
        {
            return clusterId.Value.ToString();
        }

        private string Format(ConnectionId id)
        {
            if (id.ServerValue.HasValue)
            {
                return id.LocalValue.ToString() + "-" + id.ServerValue.Value.ToString();
            }
            return id.LocalValue.ToString();
        }

        private string Format(ServerId serverId)
        {
            return Format(serverId.EndPoint);
        }

        private string Format(EndPoint endPoint)
        {
            var dnsEndPoint = endPoint as DnsEndPoint; 
            if (dnsEndPoint != null)
            {
                return string.Concat(
                    dnsEndPoint.Host,
                    ":",
                    dnsEndPoint.Port.ToString());
            }

            return endPoint.ToString();
        }

        private void Log(LogLevel level, string format, params object[] args)
        {
            if ((int)_level <= (int)level)
            {
                _writer.WriteLine(_enricher.Enrich(level, format), args);
            }
        }
    }
}