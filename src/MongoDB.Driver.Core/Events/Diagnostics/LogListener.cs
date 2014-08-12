using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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
        public override void ClusterBeforeClosing(ClusterId clusterId)
        {
            Log(LogLevel.Debug, "{0}: closing.", Label(clusterId));
        }

        public override void ClusterAfterClosing(ClusterId clusterId, TimeSpan elapsed)
        {
            Log(LogLevel.Info, "{0}: closed in {1}ms.", Label(clusterId), elapsed.TotalMilliseconds.ToString());
        }

        public override void ClusterBeforeOpening(ClusterId clusterId, ClusterSettings settings)
        {
            Log(LogLevel.Debug, "{0}: opening.", Label(clusterId));
        }

        public override void ClusterAfterOpening(ClusterId clusterId, ClusterSettings settings, TimeSpan elapsed)
        {
            Log(LogLevel.Info, "{0}: opened in {1}ms.", Label(clusterId), elapsed.TotalMilliseconds.ToString());
        }

        public override void ClusterBeforeAddingServer(ClusterId clusterId, EndPoint endPoint)
        {
            Log(LogLevel.Debug, "{0}: adding server at endpoint {1}.", Label(clusterId), Format(endPoint));
        }

        public override void ClusterAfterAddingServer(ServerId serverId, TimeSpan elapsed)
        {
            Log(LogLevel.Info, "{0}: added server {1} in {2}ms.", Label(serverId.ClusterId), Format(serverId), elapsed.TotalMilliseconds.ToString());
        }

        public override void ClusterBeforeRemovingServer(ServerId serverId, string reason)
        {
            Log(LogLevel.Debug, "{0}: removing server {1}. Reason: {2}", Label(serverId.ClusterId), Format(serverId), reason);
        }

        public override void ClusterAfterRemovingServer(ServerId serverId, string reason, TimeSpan elapsed)
        {
            Log(LogLevel.Info, "{0}: removed server {1} in {2}ms. Reason: {3}", Label(serverId.ClusterId), Format(serverId), elapsed.TotalMilliseconds.ToString(), reason);
        }

        public override void ClusterDescriptionChanged(ClusterDescription oldClusterDescription, ClusterDescription newClusterDescription)
        {
            Log(LogLevel.Info, "{0}: {1}", Label(oldClusterDescription.ClusterId), newClusterDescription);
        }

        // Servers
        public override void ServerBeforeClosing(ServerId serverId)
        {
            Log(LogLevel.Debug, "{0}: closing.", Label(serverId));
        }

        public override void ServerAfterClosing(ServerId serverId)
        {
            Log(LogLevel.Info, "{0}: closed.", Label(serverId));
        }

        public override void ServerBeforeOpening(ServerId serverId, ServerSettings settings)
        {
            Log(LogLevel.Debug, "{0}: opening.", Label(serverId));
        }

        public override void ServerAfterOpening(ServerId serverId, ServerSettings settings, TimeSpan elapsed)
        {
            Log(LogLevel.Info, "{0}: opened in {1}ms.", Label(serverId), elapsed.TotalMilliseconds);
        }

        public override void ServerBeforeHeartbeating(ConnectionId connectionId)
        {
            Log(LogLevel.Debug, "{0}: sending heartbeat.", Label(connectionId));
        }

        public override void ServerAfterHeartbeating(ConnectionId connectionId, TimeSpan elapsed)
        {
            Log(LogLevel.Debug, "{0}: sent heartbeat.", Label(connectionId));
        }

        public override void ServerErrorHeartbeating(ConnectionId connectionId, Exception exception)
        {
            Log(LogLevel.Error, "{0}: error sending heartbeat. Exception: {1}", Label(connectionId), exception);
        }

        public override void ServerAfterDescriptionChanged(ServerDescription oldDescription, ServerDescription newDescription)
        {
            Log(LogLevel.Info, "{0}: {1}", Label(oldDescription.ServerId), newDescription);
        }

        // Connection Pools
        public override void ConnectionPoolBeforeClosing(ServerId serverId)
        {
            Log(LogLevel.Debug, "{0}-pool: closing.", Label(serverId));
        }

        public override void ConnectionPoolAfterClosing(ServerId serverId)
        {
            Log(LogLevel.Info, "{0}-pool: closed.", Label(serverId));
        }

        public override void ConnectionPoolBeforeOpening(ServerId serverId, ConnectionPoolSettings settings)
        {
            Log(LogLevel.Debug, "{0}-pool: opening.", Label(serverId));
        }

        public override void ConnectionPoolAfterOpening(ServerId serverId, ConnectionPoolSettings settings)
        {
            Log(LogLevel.Info, "{0}-pool: opened.", Label(serverId));
        }

        public override void ConnectionPoolBeforeAddingAConnection(ServerId serverId)
        {
            Log(LogLevel.Debug, "{0}-pool: adding connection.", Label(serverId));
        }

        public override void ConnectionPoolAfterAddingAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
            Log(LogLevel.Info, "{0}-pool: added connection {1} in {2}ms.", Label(connectionId.ServerId), Format(connectionId), elapsed.TotalMilliseconds.ToString());
        }

        public override void ConnectionPoolBeforeRemovingAConnection(ConnectionId connectionId)
        {
            Log(LogLevel.Debug, "{0}-pool: removing connection {1}.", Label(connectionId.ServerId), Format(connectionId));
        }

        public override void ConnectionPoolAfterRemovingAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
            Log(LogLevel.Info, "{0}-pool: removed connection {1} in {2}ms.", Label(connectionId.ServerId), Format(connectionId), elapsed.TotalMilliseconds);
        }

        public override void ConnectionPoolBeforeCheckingOutAConnection(ServerId serverId)
        {
            Log(LogLevel.Debug, "{0}-pool: checking out a connection.", Label(serverId));
        }

        public override void ConnectionPoolAfterCheckingOutAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
            Log(LogLevel.Info, "{0}-pool: checked out connection {1} in {2}ms.", Label(connectionId.ServerId), Format(connectionId), elapsed.TotalMilliseconds.ToString());
        }

        public override void ConnectionPoolErrorCheckingOutAConnection(ServerId serverId, TimeSpan elapsed, Exception ex)
        {
            Log(LogLevel.Error, "{0}-pool: error checking out a connection. Exception: {1}", Label(serverId), ex);
        }

        public override void ConnectionPoolBeforeCheckingInAConnection(ConnectionId connectionId)
        {
            Log(LogLevel.Debug, "{0}-pool: checking in connection {1}.", Label(connectionId.ServerId), Format(connectionId));
        }

        public override void ConnectionPoolAfterCheckingInAConnection(ConnectionId connectionId, TimeSpan elapsed)
        {
            Log(LogLevel.Info, "{0}-pool: checked in connection {1} in {2}ms.", Label(connectionId.ServerId), Format(connectionId), elapsed.TotalMilliseconds.ToString());
        }

        // Connections
        public override void ConnectionFailed(ConnectionId connectionId, Exception ex)
        {
            Log(LogLevel.Error, "{0}: failed. Exception: {1}", Label(connectionId), ex);
        }

        public override void ConnectionBeforeClosing(ConnectionId connectionId)
        {
            Log(LogLevel.Debug, "{0}: closing.", Label(connectionId));
        }

        public override void ConnectionAfterClosing(ConnectionId connectionId)
        {
            Log(LogLevel.Info, "{0}: closed.", Label(connectionId));
        }

        public override void ConnectionBeforeOpening(ConnectionId connectionId, ConnectionSettings settings)
        {
            Log(LogLevel.Debug, "{0}: opening.", Label(connectionId));
        }

        public override void ConnectionAfterOpening(ConnectionId connectionId, ConnectionSettings settings, TimeSpan elapsed)
        {
            Log(LogLevel.Info, "{0}: opened in {1}ms.", Label(connectionId), elapsed.TotalMilliseconds.ToString());
        }

        public override void ConnectionErrorOpening(ConnectionId connectionId, Exception ex)
        {
            Log(LogLevel.Error, "{0}: unable to open. Exception: {1}", Label(connectionId), ex);
        }

        public override void ConnectionBeforeReceivingMessage(ConnectionId connectionId, int responseTo)
        {
            Log(LogLevel.Debug, "{0}: receiving message in response to {1}.", Label(connectionId), responseTo.ToString());
        }

        public override void ConnectionAfterReceivingMessage<T>(ConnectionId connectionId, ReplyMessage<T> message, int length, TimeSpan elapsed)
        {
            Log(LogLevel.Info, "{0}: received message in response to {1} of length {2} bytes in {3}ms.", Label(connectionId), message.ResponseTo.ToString(), length.ToString(), elapsed.TotalMilliseconds.ToString());
        }

        public override void ConnectionErrorReceivingMessage(ConnectionId connectionId, int responseTo, Exception ex)
        {
            Log(LogLevel.Info, "{0}: error receiving message in response to {1}. Exception: .", Label(connectionId), responseTo.ToString(), ex);
        }

        public override void ConnectionBeforeSendingMessages(ConnectionId connectionId, IReadOnlyList<RequestMessage> messages)
        {
            Log(LogLevel.Debug, "{0}: sending messages [{1}].", Label(connectionId), string.Join(",", messages.Select(x => x.RequestId)));
        }

        public override void ConnectionAfterSendingMessages(ConnectionId connectionId, IReadOnlyList<RequestMessage> messages, int length, TimeSpan elapsed)
        {
            Log(LogLevel.Info, "{0}: sent messages [{1}] of length {2} bytes in {3}ms.", Label(connectionId), string.Join(",", messages.Select(x => x.RequestId)), length.ToString(), elapsed.TotalMilliseconds.ToString());
        }

        public override void ConnectionErrorSendingMessages(ConnectionId connectionId, IReadOnlyList<RequestMessage> messages, Exception ex)
        {
            Log(LogLevel.Error, "{0}: error sending messages [{1}]. Exception: {2}", Label(connectionId), string.Join(",", messages.Select(x => x.RequestId)), ex);
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