/* Copyright 2010-present MongoDB Inc.
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

namespace MongoDB.Driver.TestApplication
{
    using System;
    using System.IO;
    using MongoDB.Driver.Core.Events;

    public class ConsoleCustomEventSubscriber : IEventSubscriber
    {
        private readonly IEventSubscriber _subscriber;

        public ConsoleCustomEventSubscriber()
        {
            _subscriber = new ReflectionEventSubscriber(this);
        }

        public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
        {
            return _subscriber.TryGetEventHandler(out handler);
        }

        #region CMAP events
        public void Handle(ConnectionPoolAddedConnectionEvent e)
        {
            Log(e.GetType().Name, $"{e.ConnectionId}:OperationId:{e.OperationId} connection added to pool at {e.Timestamp} took {e.Duration}.");
        }
        public void Handle(ConnectionPoolAddingConnectionEvent e)
        {
            Log(e.GetType().Name, $"{e.ServerId}:OperationId:{e.OperationId} connection adding to pool at {e.Timestamp}.");
        }
        public void Handle(ConnectionPoolCheckedInConnectionEvent e)
        {
            Log(e.GetType().Name, $"{e.ConnectionId}:OperationId:{e.OperationId} checked in connection to pool at {e.Timestamp} took {e.Duration}.");
        }
        public void Handle(ConnectionPoolCheckedOutConnectionEvent e)
        {
            Log(e.GetType().Name, $"{e.ConnectionId}:OperationId:{e.OperationId} checked out connection from pool at {e.Timestamp} took {e.Duration}.");
        }
        public void Handle(ConnectionPoolCheckingInConnectionEvent e)
        {
            Log(e.GetType().Name, $"{e.ConnectionId}:OperationId:{e.OperationId} checking in connection to pool at {e.Timestamp}.");
        }
        public void Handle(ConnectionPoolCheckingOutConnectionEvent e)
        {
            Log(e.GetType().Name, $"{e.ServerId}:OperationId:{e.OperationId} checking out connection from pool at {e.Timestamp}.");
        }
        public void Handle(ConnectionPoolCheckingOutConnectionFailedEvent e)
        {
            Log(e.GetType().Name, $"{e.ServerId}:OperationId:{e.OperationId} failed to check out connection for operation {e.OperationId} at {e.Timestamp}: {e.Reason}. Exception: {e.Exception}");
        }
        public void Handle(ConnectionPoolClearedEvent e)
        {
            Log(e.GetType().Name, $"{e.ServerId} pool cleared at {e.Timestamp}.");
        }
        public void Handle(ConnectionPoolClearingEvent e)
        {
            Log(e.GetType().Name, $"{e.ServerId} pool clearing at {e.Timestamp}.");
        }
        public void Handle(ConnectionPoolClosedEvent e)
        {
            Log(e.GetType().Name, $"{e.ServerId} pool closed at {e.Timestamp}.");
        }
        public void Handle(ConnectionPoolClosingEvent e)
        {
            Log(e.GetType().Name, $"{e.ServerId} pool closing at {e.Timestamp}.");
        }
        public void Handle(ConnectionPoolOpenedEvent e)
        {
            Log(e.GetType().Name, $"{e.ServerId} pool opened at {e.Timestamp}.");
        }
        public void Handle(ConnectionPoolOpeningEvent e)
        {
            Log(e.GetType().Name, $"{e.ServerId} pool opening at {e.Timestamp}.");
        }
        public void Handle(ConnectionPoolRemovedConnectionEvent e)
        {
            Log(e.GetType().Name, $"{e.ConnectionId}:OperationId:{e.OperationId} connection removed from pool at {e.Timestamp} took {e.Duration}.");
        }
        public void Handle(ConnectionPoolRemovingConnectionEvent e)
        {
            Log(e.GetType().Name, $"{e.ConnectionId}:OperationId:{e.OperationId} connection removing from pool at {e.Timestamp}.");
        }
        #endregion CMAP events

        #region Command events
        public void Handle(CommandStartedEvent e)
        {
            Log(e.GetType().Name, $"{e.ConnectionId}:OperationId:{e.OperationId} started {e.CommandName} on {e.DatabaseNamespace} at {e.Timestamp}.");
        }
        public void Handle(CommandSucceededEvent e)
        {
            Log(e.GetType().Name, $"{e.ConnectionId}:OperationId:{e.OperationId} {e.CommandName} succeeded at {e.Timestamp} took {e.Duration}.");
        }
        public void Handle(CommandFailedEvent e)
        {
            Log(e.GetType().Name, $"{e.ConnectionId}:OperationId:{e.OperationId} {e.CommandName} failed at {e.Timestamp} took {e.Duration} ex: {e.Failure}.");
        }
        #endregion Command events

        #region SDAM events
        public void Handle(ClusterAddedServerEvent e)
        {
            Log(e.GetType().Name, $"{e.ClusterId} added server {e.ServerId} at {e.Timestamp} took {e.Duration}.");
        }
        public void Handle(ClusterAddingServerEvent e)
        {
            Log(e.GetType().Name, $"{e.ClusterId} adding server {e.EndPoint} at {e.Timestamp}.");
        }
        public void Handle(ClusterClosedEvent e)
        {
            Log(e.GetType().Name, $"{e.ClusterId} closed at {e.Timestamp} took {e.Duration}");
        }
        public void Handle(ClusterClosingEvent e)
        {
            Log(e.GetType().Name, $"{e.ClusterId} closing at {e.Timestamp}");
        }
        public void Handle(ClusterDescriptionChangedEvent e)
        {
            Log(e.GetType().Name, $"{e.ClusterId} topology changed at {e.Timestamp} from {e.OldDescription} to {e.NewDescription}.");
        }
        public void Handle(ClusterOpenedEvent e)
        {
            Log(e.GetType().Name, $"{e.ClusterId} opened at {e.Timestamp} and took {e.Duration}.");
        }
        public void Handle(ClusterOpeningEvent e)
        {
            Log(e.GetType().Name, $"{e.ClusterId} opening at {e.Timestamp}.");
        }
        public void Handle(ClusterRemovedServerEvent e)
        {
            Log(e.GetType().Name, $"{e.ClusterId} removed server {e.ServerId} at {e.Timestamp} took {e.Duration}.");
        }
        public void Handle(ClusterRemovingServerEvent e)
        {
            Log(e.GetType().Name, $"{e.ClusterId} removing server {e.ServerId} at {e.Timestamp}.");
        }
        public void Handle(SdamInformationEvent e)
        {
            Log(e.GetType().Name, $"{e.Message} received at {e.Timestamp}.");
        }
        public void Handle(ServerClosedEvent e)
        {
            Log(e.GetType().Name, $"{e.ClusterId} closed server {e.ServerId} at {e.Timestamp} took {e.Duration}.");
        }
        public void Handle(ServerClosingEvent e)
        {
            Log(e.GetType().Name, $"{e.ClusterId} closing server {e.ServerId} at {e.Timestamp}.");
        }
        public void Handle(ServerDescriptionChangedEvent e)
        {
            Log(e.GetType().Name, $"{e.ClusterId} server description changed at {e.Timestamp} from {e.OldDescription} to {e.NewDescription}.");
        }
        public void Handle(ServerHeartbeatFailedEvent e)
        {
            Log(e.GetType().Name, $"{e.ServerId}/{e.ConnectionId} failed heartbeat at {e.Timestamp}. Exception: {e.Exception}.");
        }
        public void Handle(ServerHeartbeatStartedEvent e)
        {
            Log(e.GetType().Name, $"{e.ServerId}/{e.ConnectionId} started heartbeat at {e.Timestamp}.");
        }
        public void Handle(ServerHeartbeatSucceededEvent e)
        {
            Log(e.GetType().Name, $"{e.ServerId}/{e.ConnectionId} succeeded heartbeat at {e.Timestamp} took {e.Duration}.");
        }
        public void Handle(ServerOpenedEvent e)
        {
            Log(e.GetType().Name, $"{e.ClusterId} opened server {e.ServerId} at {e.Timestamp} took {e.Duration}.");
        }
        public void Handle(ServerOpeningEvent e)
        {
            Log(e.GetType().Name, $"{e.ClusterId} opening server {e.ServerId} at {e.Timestamp}.");
        }
        #endregion SDAM events

        public void Handle(DiagnosticEvent e)
        {
            Log("Diagnostic Event", $"{e.Message}. Timestamp: {e.Timestamp}.");
        }

        private void Log(string eventName, string eventDetails)
        {
            Console.WriteLine($"{DateTime.UtcNow:O}\t{eventName}\t{eventDetails}");
        }
    }

    class Program
    {
        static string __logFileName = @"c:\Logs\log.txt";

        static StreamWriter ConfigureConsoleOutputToFile(FileStream fileStream)
        {
            FileStream oldStream;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            try
            {
                writer = new StreamWriter(fileStream);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Cannot open {__logFileName} for writing. Exception: " + e);
                throw;
            }

            Console.SetOut(writer);
            return writer;
        }

        static void Main(string[] args)
        {
            const string MONGODB_URI = "<connection_string>";
            var url = new MongoUrl(MONGODB_URI);
            var settings = MongoClientSettings.FromUrl(url);

            // The below options were added for this POC and might not be presented in the evential production version.
            // They were configured to allow driver to notice earlier that server has failed.
            // - RttInterval - reduce default 10 seconds RTT interval. This means that driver will ask server about his state at least each 5 seconds
            // - RttReadTimeout - reduce default 30 seconds socket read timeout. Now any RTT health check will fail if operation didn't return result after 3 seconds.
            MongoInternalDefaults.RttInterval = TimeSpan.FromSeconds(5);
            MongoInternalDefaults.RttReadTimeout = TimeSpan.FromSeconds(3);

            // make sure that the below delegate is created only once.
            // If you don't use a singletone pattern, it's better to pass the same instance of this delegate during all MongoClientSettings creation.
            settings.ClusterConfigurator = cb =>
            {
                cb.Subscribe(new ConsoleCustomEventSubscriber()); // this subscriber works in pair with below fileStream configuration
            };

            // configure console output into log file
            using var fileStream = new FileStream(__logFileName, FileMode.OpenOrCreate, FileAccess.Write);
            using var writeStream = ConfigureConsoleOutputToFile(fileStream);

            var client = new MongoClient(settings);
            // normal driver usage
        }
    }
}
