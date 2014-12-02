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
        public override void BeforeClosing(ClusterBeforeClosingEvent @event)
        {
            Log(LogLevel.Debug, "{0}: closing.", Label(@event.ClusterId));
        }

        public override void AfterClosing(ClusterAfterClosingEvent @event)
        {
            Log(LogLevel.Info, "{0}: closed in {1}ms.", Label(@event.ClusterId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void BeforeOpening(ClusterBeforeOpeningEvent @event)
        {
            Log(LogLevel.Debug, "{0}: opening.", Label(@event.ClusterId));
        }

        public override void AfterOpening(ClusterAfterOpeningEvent @event)
        {
            Log(LogLevel.Info, "{0}: opened in {1}ms.", Label(@event.ClusterId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void BeforeAddingServer(ClusterBeforeAddingServerEvent @event)
        {
            Log(LogLevel.Debug, "{0}: adding server at endpoint {1}.", Label(@event.ClusterId), Format(@event.EndPoint));
        }

        public override void AfterAddingServer(ClusterAfterAddingServerEvent @event)
        {
            Log(LogLevel.Info, "{0}: added server {1} in {2}ms.", Label(@event.ServerId.ClusterId), Format(@event.ServerId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void BeforeRemovingServer(ClusterBeforeRemovingServerEvent @event)
        {
            Log(LogLevel.Debug, "{0}: removing server {1}. Reason: {2}", Label(@event.ServerId.ClusterId), Format(@event.ServerId), @event.Reason);
        }

        public override void AfterRemovingServer(ClusterAfterRemovingServerEvent @event)
        {
            Log(LogLevel.Info, "{0}: removed server {1} in {2}ms. Reason: {3}", Label(@event.ServerId.ClusterId), Format(@event.ServerId), @event.Elapsed.TotalMilliseconds.ToString(), @event.Reason);
        }

        public override void AfterDescriptionChanged(ClusterAfterDescriptionChangedEvent @event)
        {
            Log(LogLevel.Info, "{0}: {1}", Label(@event.OldDescription.ClusterId), @event.NewDescription);
        }

        // Servers
        public override void BeforeClosing(ServerBeforeClosingEvent @event)
        {
            Log(LogLevel.Debug, "{0}: closing.", Label(@event.ServerId));
        }

        public override void AfterClosing(ServerAfterClosingEvent @event)
        {
            Log(LogLevel.Info, "{0}: closed in {1}ms.", Label(@event.ServerId), @event.Elapsed.TotalMilliseconds);
        }

        public override void BeforeOpening(ServerBeforeOpeningEvent @event)
        {
            Log(LogLevel.Debug, "{0}: opening.", Label(@event.ServerId));
        }

        public override void AfterOpening(ServerAfterOpeningEvent @event)
        {
            Log(LogLevel.Info, "{0}: opened in {1}ms.", Label(@event.ServerId), @event.Elapsed.TotalMilliseconds);
        }

        public override void BeforeHeartbeating(ServerBeforeHeartbeatingEvent @event)
        {
            Log(LogLevel.Debug, "{0}: sending heartbeat.", Label(@event.ConnectionId));
        }

        public override void AfterHeartbeating(ServerAfterHeartbeatingEvent @event)
        {
            Log(LogLevel.Debug, "{0}: sent heartbeat in {1}ms.", Label(@event.ConnectionId), @event.Elapsed.TotalMilliseconds);
        }

        public override void ErrorHeartbeating(ServerErrorHeartbeatingEvent @event)
        {
            Log(LogLevel.Error, "{0}: error sending heartbeat. Exception: {1}", Label(@event.ConnectionId), @event.Exception);
        }

        public override void AfterDescriptionChanged(ServerAfterDescriptionChangedEvent @event)
        {
            Log(LogLevel.Info, "{0}: {1}", Label(@event.OldDescription.ServerId), @event.NewDescription);
        }

        // Connection Pools
        public override void BeforeClosing(ConnectionPoolBeforeClosingEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: closing.", Label(@event.ServerId));
        }

        public override void AfterClosing(ConnectionPoolAfterClosingEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: closed.", Label(@event.ServerId));
        }

        public override void BeforeOpening(ConnectionPoolBeforeOpeningEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: opening.", Label(@event.ServerId));
        }

        public override void AfterOpening(ConnectionPoolAfterOpeningEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: opened.", Label(@event.ServerId));
        }

        public override void BeforeAddingAConnection(ConnectionPoolBeforeAddingAConnectionEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: adding connection.", Label(@event.ServerId));
        }

        public override void AfterAddingAConnection(ConnectionPoolAfterAddingAConnectionEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: added connection {1} in {2}ms.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void BeforeRemovingAConnection(@ConnectionPoolBeforeRemovingAConnectionEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: removing connection {1}.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId));
        }

        public override void AfterRemovingAConnection(ConnectionPoolAfterRemovingAConnectionEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: removed connection {1} in {2}ms.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId), @event.Elapsed.TotalMilliseconds);
        }

        public override void BeforeCheckingOutAConnection(ConnectionPoolBeforeCheckingOutAConnectionEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: checking out a connection.", Label(@event.ServerId));
        }

        public override void AfterCheckingOutAConnection(ConnectionPoolAfterCheckingOutAConnectionEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: checked out connection {1} in {2}ms.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void ErrorCheckingOutAConnection(ConnectionPoolErrorCheckingOutAConnectionEvent @event)
        {
            Log(LogLevel.Error, "{0}-pool: error checking out a connection. Exception: {1}", Label(@event.ServerId), @event.Exception);
        }

        public override void BeforeCheckingInAConnection(ConnectionPoolBeforeCheckingInAConnectionEvent @event)
        {
            Log(LogLevel.Debug, "{0}-pool: checking in connection {1}.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId));
        }

        public override void AfterCheckingInAConnection(ConnectionPoolAfterCheckingInAConnectionEvent @event)
        {
            Log(LogLevel.Info, "{0}-pool: checked in connection {1} in {2}ms.", Label(@event.ConnectionId.ServerId), Format(@event.ConnectionId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        // Connections
        public override void Failed(ConnectionFailedEvent @event)
        {
            Log(LogLevel.Error, "{0}: failed. Exception: {1}", Label(@event.ConnectionId), @event.Exception);
        }

        public override void BeforeClosing(ConnectionBeforeClosingEvent @event)
        {
            Log(LogLevel.Debug, "{0}: closing.", Label(@event.ConnectionId));
        }

        public override void AfterClosing(ConnectionAfterClosingEvent @event)
        {
            Log(LogLevel.Info, "{0}: closed.", Label(@event.ConnectionId));
        }

        public override void BeforeOpening(ConnectionBeforeOpeningEvent @event)
        {
            Log(LogLevel.Debug, "{0}: opening.", Label(@event.ConnectionId));
        }

        public override void AfterOpening(ConnectionAfterOpeningEvent @event)
        {
            Log(LogLevel.Info, "{0}: opened in {1}ms.", Label(@event.ConnectionId), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void ErrorOpening(ConnectionErrorOpeningEvent @event)
        {
            Log(LogLevel.Error, "{0}: unable to open. Exception: {1}", Label(@event.ConnectionId), @event.Exception);
        }

        public override void BeforeReceivingMessage(ConnectionBeforeReceivingMessageEvent @event)
        {
            Log(LogLevel.Debug, "{0}: receiving message in response to {1}.", Label(@event.ConnectionId), @event.ResponseTo.ToString());
        }

        public override void AfterReceivingMessage<T>(ConnectionAfterReceivingMessageEvent<T> @event)
        {
            Log(LogLevel.Info, "{0}: received message in response to {1} of length {2} bytes in {3}ms.", Label(@event.ConnectionId), @event.ReplyMessage.ResponseTo.ToString(), @event.Length.ToString(), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void ErrorReceivingMessage(ConnectionErrorReceivingMessageEvent @event)
        {
            Log(LogLevel.Info, "{0}: error receiving message in response to {1}. Exception: .", Label(@event.ConnectionId), @event.ResponseTo.ToString(), @event.Exception);
        }

        public override void BeforeSendingMessages(ConnectionBeforeSendingMessagesEvent @event)
        {
            Log(LogLevel.Debug, "{0}: sending messages [{1}].", Label(@event.ConnectionId), string.Join(",", @event.Messages.Select(x => x.RequestId)));
        }

        public override void AfterSendingMessages(ConnectionAfterSendingMessagesEvent @event)
        {
            Log(LogLevel.Info, "{0}: sent messages [{1}] of length {2} bytes in {3}ms.", Label(@event.ConnectionId), string.Join(",", @event.Messages.Select(x => x.RequestId)), @event.Length.ToString(), @event.Elapsed.TotalMilliseconds.ToString());
        }

        public override void ErrorSendingMessages(ConnectionErrorSendingMessagesEvent @event)
        {
            Log(LogLevel.Error, "{0}: error sending messages [{1}]. Exception: {2}", Label(@event.ConnectionId), string.Join(",", @event.Messages.Select(x => x.RequestId)), @event.Exception);
            base.ErrorSendingMessages(@event);
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