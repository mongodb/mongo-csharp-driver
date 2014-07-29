using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Async;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders;

namespace MongoDB.Driver.Core.TestConsoleApplication
{
    public class Logger : IClusterListener, IServerListener, IMessageListener
    {
        #region static
        private static Task __done = Task.FromResult(true);
        private static int __id = 0;
        #endregion

        // fields
        private readonly TextWriter[] _destinations;

        // construtors
        public Logger(IEnumerable<TextWriter> destinations)
        {
            _destinations = Ensure.IsNotNull(destinations, "destinations").ToArray();
        }

        public Logger(params TextWriter[] destinations)
            : this((IEnumerable<TextWriter>)destinations)
        {
        }

        // methods
        public void ClusterDescriptionChanged(ClusterDescriptionChangedEventArgs args)
        {
        }

        private void LogMessage(string eventType, DnsEndPoint endPoint, MongoDBMessage message, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var encoderFactory = new JsonMessageEncoderFactory(jsonWriter);
                var encoder = message.GetEncoder(encoderFactory);
                encoder.WriteMessage(message);
                var jsonMessage = stringWriter.ToString();
                var logMessage = string.Format("EndPoint : '{0}', Message : {1}", DnsEndPointParser.ToString(endPoint), jsonMessage);
                WriteLog(eventType, logMessage, timeout, cancellationToken);
            }
        }


        public void ReceivedMessage(ReceivedMessageEventArgs args)
        {
            LogMessage("ReceivedMessage", args.EndPoint, args.Reply);
        }

        public void SendingHeartbeat(SendingHeartbeatEventArgs args)
        {
            var endPoint = args.EndPoint;
            var logMessage = string.Format("EndPoint : \"{0}\"", DnsEndPointParser.ToString(endPoint));
            WriteLog("SendingHeartbeat", logMessage);
        }

        public void SendingMessage(SendingMessageEventArgs args)
        {
            LogMessage("SendingMessage", args.EndPoint, args.Message);
        }

        public void SentHeartbeat(SentHeartbeatEventArgs args)
        {
            var endPoint = args.EndPoint;
            var logMessage = string.Format("EndPoint : \"{0}\", isMaster : {1}, buildInfo : {2}",
                DnsEndPointParser.ToString(endPoint),
                args.IsMasterResult.Wrapped.ToJson(),
                args.BuildInfoResult.Wrapped.ToJson());
            WriteLog("SentHeartbeat", logMessage);
        }

        public void SentMessage(SentMessageEventArgs args)
        {
            LogMessage("SentMessage", args.EndPoint, args.Message);
        }

        public void ServerAdded(ServerAddedEventArgs args)
        {
            var endPoint = args.ServerDescription.EndPoint;
            var logMessage = string.Format("EndPoint : '{0}'", DnsEndPointParser.ToString(endPoint));
            WriteLog("ServerAdded", logMessage);
        }

        public void ServerDescriptionChanged(ServerDescriptionChangedEventArgs args)
        {
            var oldServerDescription = args.OldServerDescription;
            var newServerDescription = args.NewServerDescription;
            var endPoint = newServerDescription.EndPoint;
            var logMessage = string.Format("EndPoint : '{0}', NewServerDescription : {1}, OldServerDescription : {1}",
                DnsEndPointParser.ToString(endPoint),
                newServerDescription.ToString(),
                oldServerDescription.ToString());
            WriteLog("ServerDescriptionChanged", logMessage);
        }

        public void ServerRemoved(ServerRemovedEventArgs args)
        {
            var endPoint = args.EndPoint;
            var logMessage = string.Format("EndPoint : '{0}'", DnsEndPointParser.ToString(endPoint));
            WriteLog("ServerRemoved", logMessage);
        }

        private void WriteLog(string eventType, string message, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            var id = Interlocked.Increment(ref __id);
            var timestamp = DateTime.Now.ToString("s");
            var timestampedMessage = string.Format("{{ _id : {0}, timestamp : ISODate('{1}'), event : {2}, {3} }}", id, timestamp, eventType, message);

            var slidingTimeout = new SlidingTimeout(timeout);
            foreach (var destination in _destinations)
            {
                destination.WriteLine(timestampedMessage);
                destination.Flush();
            }
        }
    }
}
