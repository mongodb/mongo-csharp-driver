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
    public class LogListener : EmptyListener
    {
        private readonly LogEnricher _enricher;
        private readonly LogLevel _level;
        private readonly TextWriter _logger;

        public LogListener(TextWriter logger, LogLevel level = LogLevel.Info, LogEnricher enricher = null)
        {
            _logger = Ensure.IsNotNull(logger, "logger");
            _level = level;
            _enricher = enricher ?? new LogEnricher();
        }

        // Clusters
        public override void ClusterBeforeClosing(ClusterBeforeClosingEvent @event)
        {
            Log(LogLevel.Debug, "{0}: closing.", Label(@event.ClusterId));
        }

        public override void ClusterAfterClosing(ClusterAfterClosingEvent @event)
        {
            Log(LogLevel.Info, "{0}: closed in {1}ms.", Label(@event.ClusterId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void ClusterBeforeOpening(ClusterBeforeOpeningEvent @event)
        {
            Log(LogLevel.Debug, "{0}: opening.", Label(@event.ClusterId));
        }

        public override void ClusterAfterOpening(ClusterAfterOpeningEvent @event)
        {
            Log(LogLevel.Info, "{0}: opened in {1}ms.", Label(@event.ClusterId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void ClusterBeforeAddingServer(ClusterBeforeAddingServerEvent @event)
        {
            Log(LogLevel.Debug, "{0}: adding server at endpoint {1}.", Label(@event.ClusterId), Format(@event.EndPoint));
        }

        public override void ClusterAfterAddingServer(ClusterAfterAddingServerEvent @event)
        {
            Log(LogLevel.Info, "{0}: added server {1} in {2}ms.", Label(@event.ServerId.ClusterId), Format(@event.ServerId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void ClusterBeforeRemovingServer(ClusterBeforeRemovingServerEvent @event)
        {
            Log(LogLevel.Debug, "{0}: removing server {1}. Reason: {2}", Label(@event.ServerId.ClusterId), Format(@event.ServerId), @event.Reason);
        }

        public override void ClusterAfterRemovingServer(ClusterAfterRemovingServerEvent @event)
        {
            Log(LogLevel.Info, "{0}: removed server {1} in {2}ms. Reason: {3}", Label(@event.ServerId.ClusterId), Format(@event.ServerId), @event.Elapsed.TotalMilliseconds.ToString(), @event.Reason);
        }

        public override void ClusterAfterDescriptionChanged(ClusterAfterDescriptionChangedEvent @event)
        {
            Log(LogLevel.Info, "{0}: {1}", Label(@event.OldDescription.ClusterId), @event.NewDescription);
        }

        // Servers
        public override void ServerBeforeClosing(ServerBeforeClosingEvent @event)
        {
            Log(LogLevel.Debug, "{0}: closing.", Label(@event.ServerId));
        }

        public override void ServerAfterClosing(ServerAfterClosingEvent @event)
        {
            Log(LogLevel.Info, "{0}: closed in {1}ms.", Label(@event.ServerId), @event.Elapsed.TotalMilliseconds);
        }

        public override void ServerBeforeOpening(ServerBeforeOpeningEvent @event)
        {
            Log(LogLevel.Debug, "{0}: opening.", Label(@event.ServerId));
        }

        public override void ServerAfterOpening(ServerAfterOpeningEvent @event)
        {
            Log(LogLevel.Info, "{0}: opened in {1}ms.", Label(@event.ServerId), @event.Elapsed.TotalMilliseconds);
        }

        public override void ServerBeforeHeartbeating(ServerBeforeHeartbeatingEvent @event)
        {
            Log(LogLevel.Debug, "{0}: sending heartbeat.", Label(@event.ConnectionId));
        }

        public override void ServerAfterHeartbeating(ServerAfterHeartbeatingEvent @event)
        {
            Log(LogLevel.Debug, "{0}: sent heartbeat in {1}ms.", Label(@event.ConnectionId), @event.Elapsed.TotalMilliseconds);
        }

        public override void ServerErrorHeartbeating(ServerErrorHeartbeatingEvent @event)
        {
            Log(LogLevel.Error, "{0}: error sending heartbeat. Exception: {1}", Label(@event.ConnectionId), @event.Exception);
        }

        public override void ServerAfterDescriptionChanged(ServerAfterDescriptionChangedEvent @event)
        {
            Log(LogLevel.Info, "{0}: {1}", Label(@event.OldDescription.ServerId), @event.NewDescription);
        }

        // Connection Pools
        public override void ConnectionPoolBeforeClosing(ConnectionPoolBeforeClosingEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: closing.", Label(@event.ServerId));
        }

        public override void ConnectionPoolAfterClosing(ConnectionPoolAfterClosingEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: closed.", Label(@event.ServerId));
        }

        public override void ConnectionPoolBeforeOpening(ConnectionPoolBeforeOpeningEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: opening.", Label(@event.ServerId));
        }

        public override void ConnectionPoolAfterOpening(ConnectionPoolAfterOpeningEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: opened.", Label(@event.ServerId));
        }

        public override void ConnectionPoolBeforeAddingAConnection(ConnectionPoolBeforeAddingAConnectionEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: adding connection.", Label(@event.ServerId));
        }

        public override void ConnectionPoolAfterAddingAConnection(ConnectionPoolAfterAddingAConnectionEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: added connection {1} in {2}ms.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void ConnectionPoolBeforeRemovingAConnection(@ConnectionPoolBeforeRemovingAConnectionEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: removing connection {1}.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId));
        }

        public override void ConnectionPoolAfterRemovingAConnection(ConnectionPoolAfterRemovingAConnectionEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: removed connection {1} in {2}ms.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId), @event.Elapsed.TotalMilliseconds);
        }

        public override void ConnectionPoolBeforeCheckingOutAConnection(ConnectionPoolBeforeCheckingOutAConnectionEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: checking out a connection.", Label(@event.ServerId));
        }

        public override void ConnectionPoolAfterCheckingOutAConnection(ConnectionPoolAfterCheckingOutAConnectionEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: checked out connection {1} in {2}ms.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void ConnectionPoolErrorCheckingOutAConnection(ConnectionPoolErrorCheckingOutAConnectionEvent @event)
        {
            Log(LogLevel.Error, "{0}-pool: error checking out a connection. Exception: {1}", Label(@event.ServerId), @event.Exception);
        }

        public override void ConnectionPoolBeforeCheckingInAConnection(ConnectionPoolBeforeCheckingInAConnectionEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: checking in connection {1}.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId));
        }

        public override void ConnectionPoolAfterCheckingInAConnection(ConnectionPoolAfterCheckingInAConnectionEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: checked in connection {1} in {2}ms.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        // Connections
        public override void ConnectionFailed(ConnectionFailedEvent @event)
        {
            Log(LogLevel.Error, "{0}: failed. Exception: {1}", Label(@event.ConnectionId), @event.Exception);
        }

        public override void ConnectionBeforeClosing(ConnectionBeforeClosingEvent @event)
        {
            Log(LogLevel.Debug, "{0}: closing.", Label(@event.ConnectionId));
        }

        public override void ConnectionAfterClosing(ConnectionAfterClosingEvent @event)
        {
            Log(LogLevel.Info, "{0}: closed.", Label(@event.ConnectionId));
        }

        public override void ConnectionBeforeOpening(ConnectionBeforeOpeningEvent @event)
        {
            Log(LogLevel.Debug, "{0}: opening.", Label(@event.ConnectionId));
        }

        public override void ConnectionAfterOpening(ConnectionAfterOpeningEvent @event)
        {
            Log(LogLevel.Info, "{0}: opened in {1}ms.", Label(@event.ConnectionId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void ConnectionErrorOpening(ConnectionErrorOpeningEvent @event)
        {
            Log(LogLevel.Error, "{0}: unable to open. Exception: {1}", Label(@event.ConnectionId), @event.Exception);
        }

        public override void ConnectionBeforeReceivingMessage(ConnectionBeforeReceivingMessageEvent @event)
        {
            Log(LogLevel.Debug, "{0}: receiving message in response to {1}.", Label(@event.ConnectionId), @event.ResponseTo.ToString());
        }

        public override void ConnectionAfterReceivingMessage<T>(ConnectionAfterReceivingMessageEvent<T> @event)
        {
            Log(LogLevel.Info, "{0}: received message in response to {1} of length {2} bytes in {3}ms.", Label(@event.ConnectionId), @event.ReplyMessage.ResponseTo.ToString(), @event.Length.ToString(), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void ConnectionErrorReceivingMessage(ConnectionErrorReceivingMessageEvent @event)
        {
            Log(LogLevel.Info, "{0}: error receiving message in response to {1}. Exception: .", Label(@event.ConnectionId), @event.ResponseTo.ToString(), @event.Exception);
        }

        public override void ConnectionBeforeSendingMessages(ConnectionBeforeSendingMessagesEvent @event)
        {
            Log(LogLevel.Debug, "{0}: sending messages [{1}].", Label(@event.ConnectionId), string.Join(",", @event.Messages.Select(x => x.RequestId)));
        }

        public override void ConnectionAfterSendingMessages(ConnectionAfterSendingMessagesEvent @event)
        {
            Log(LogLevel.Info, "{0}: sent messages [{1}] of length {2} bytes in {3}ms.", Label(@event.ConnectionId), string.Join(",", @event.Messages.Select(x => x.RequestId)), @event.Length.ToString(), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void ConnectionErrorSendingMessages(ConnectionErrorSendingMessagesEvent @event)
        {
            Log(LogLevel.Error, "{0}: error sending messages [{1}]. Exception: {2}", Label(@event.ConnectionId), string.Join(",", @event.Messages.Select(x => x.RequestId)), @event.Exception);
            base.ConnectionErrorSendingMessages(@event);
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
            return string.Format("{0}:{1}", serverId.ClusterId.Value.ToString(), Format(serverId.EndPoint));
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
                return dnsEndPoint.Host + ":" + dnsEndPoint.Port.ToString();
            }

            var ipEndPoint = endPoint as IPEndPoint;
            if (ipEndPoint != null)
            {
                return ipEndPoint.Address.ToString() + ":" + ipEndPoint.Port.ToString();
            }

            return endPoint.ToString();
        }

        private void Log(LogLevel level, string format, params object[] args)
        {
            if ((int)_level <= (int)level)
            {
                _logger.WriteLine(_enricher.Enrich(level, format), args);
            }
        }
    }
}