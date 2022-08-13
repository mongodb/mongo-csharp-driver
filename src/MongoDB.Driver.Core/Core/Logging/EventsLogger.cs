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

using System;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Events;
using static MongoDB.Driver.Core.Logging.StructuredLogsTemplates;

namespace MongoDB.Driver.Core.Logging
{
    internal sealed class EventsLogger<T> : LoggerDecorator<T>
        where T : LogCategories.EventCategory
    {
        private readonly EventsPublisher _eventsPublisher;
        private bool _isInformationEnabled;

        public EventsLogger(IEventSubscriber eventSubscriber, ILogger<T> logger, string id) :
            base(logger, IdParameter, id)
        {
            _eventsPublisher = eventSubscriber != null ? new EventsPublisher(eventSubscriber) : null;
            _isInformationEnabled = Logger?.IsEnabled(LogLevel.Information) == true;
        }

        private string Id => DecorationValue;

        // All events have information log level
        public bool IsEventTracked<TEvent>() => _isInformationEnabled || _eventsPublisher?.IsEventTracked<TEvent>() == true;

        #region Command

        public void LogAndPublish(CommandStartedEvent @event)
        {
            if (_isInformationEnabled)
            {
                Logger.LogInformation(Id_Message_RequestId_CommandName_Command,
                    Id,
                    "Command started",
                    @event.RequestId,
                    @event.CommandName,
                    @event.Command.ToString()); // Convert command to string, as BsonDocument can be disposed
            }

            _eventsPublisher?.Publish(EventType.CommandStarted, @event);
        }

        public void LogAndPublish(CommandSucceededEvent @event)
        {
            Logger?.LogInformation(Id_Message_RequestId_CommandName,
                Id,
                "Command succeeded",
                @event.RequestId,
                @event.CommandName);

            _eventsPublisher?.Publish(EventType.CommandSucceeded, @event);
        }

        public void LogAndPublish(CommandFailedEvent @event)
        {
            Logger?.LogInformation(@event.Failure, Id_Message_RequestId_CommandName, Id, "Command failed", @event.RequestId, @event.CommandName);

            _eventsPublisher?.Publish(EventType.CommandFailed, @event);
        }

        #endregion

        #region Connection

        public void LogAndPublish(ConnectionFailedEvent @event)
        {
            Logger?.LogInformation(Id_Message_ServerId, Id, "Failed", @event.ServerId);

            _eventsPublisher?.Publish(EventType.ConnectionFailed, @event);
        }

        public void LogAndPublish(ConnectionClosingEvent @event)
        {
            Logger?.LogInformation(Id_Message_ServerId, Id, "Closing", @event.ServerId);

            _eventsPublisher?.Publish(EventType.ConnectionClosing, @event);
        }

        public void LogAndPublish(ConnectionClosedEvent @event)
        {
            Logger?.LogInformation(Id_Message_ServerId, Id, "Closed", @event.ServerId);

            _eventsPublisher?.Publish(EventType.ConnectionClosed, @event);
        }

        public void LogAndPublish(ConnectionOpeningEvent @event)
        {
            Logger?.LogInformation(Id_Message_ServerId, Id, "Opening", @event.ServerId);

            _eventsPublisher?.Publish(EventType.ConnectionOpening, @event);
        }

        public void LogAndPublish(ConnectionOpenedEvent @event)
        {
            Logger?.LogInformation(Id_Message_ServerId, Id, "Opened", @event.ServerId);

            _eventsPublisher?.Publish(EventType.ConnectionOpened, @event);
        }

        public void LogAndPublish(ConnectionOpeningFailedEvent @event)
        {
            Logger?.LogInformation(@event.Exception, Id_Message_ServerId, Id, "Opening failed", @event.ServerId);

            _eventsPublisher?.Publish(EventType.ConnectionOpeningFailed, @event);
        }

        public void LogAndPublish(ConnectionReceivingMessageEvent @event)
        {
            Logger?.LogInformation(Id_Message_ServerId, Id, "Receiving", @event.ServerId);

            _eventsPublisher?.Publish(EventType.ConnectionReceivingMessage, @event);
        }

        public void LogAndPublish(ConnectionReceivedMessageEvent @event)
        {
            Logger?.LogInformation(Id_Message_ServerId, Id, "Received", @event.ServerId);

            _eventsPublisher?.Publish(EventType.ConnectionReceivedMessage, @event);
        }

        public void LogAndPublish(ConnectionReceivingMessageFailedEvent @event)
        {
            Logger?.LogInformation(@event.Exception, Id_Message_ServerId, Id, "Receiving failed", @event.ServerId);

            _eventsPublisher?.Publish(EventType.ConnectionReceivingMessageFailed, @event);
        }

        public void LogAndPublish(ConnectionSendingMessagesEvent @event)
        {
            Logger?.LogInformation(Id_Message_ServerId, Id, "Sending", @event.ServerId);

            _eventsPublisher?.Publish(EventType.ConnectionSendingMessages, @event);
        }

        public void LogAndPublish(ConnectionSentMessagesEvent @event)
        {
            Logger?.LogInformation(Id_Message_ServerId, Id, "Sent", @event.ServerId);

            _eventsPublisher?.Publish(EventType.ConnectionSentMessages, @event);
        }

        public void LogAndPublish(ConnectionSendingMessagesFailedEvent @event)
        {
            Logger?.LogInformation(@event.Exception, Id_Message_ServerId, Id, "Sending failed", @event.ServerId);

            _eventsPublisher?.Publish(EventType.ConnectionSendingMessagesFailed, @event);
        }

        #endregion

        #region CMAP

        public void LogAndPublish(ConnectionPoolCheckingOutConnectionEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Checking out connection");

            _eventsPublisher?.Publish(EventType.ConnectionPoolCheckingOutConnection, @event);
        }

        public void LogAndPublish(ConnectionPoolCheckedOutConnectionEvent @event)
        {
            Logger?.LogInformation(Id_Message_ConnectionId, Id, "Checked out connection",
                @event.ConnectionId);

            _eventsPublisher?.Publish(EventType.ConnectionPoolCheckedOutConnection, @event);
        }

        public void LogAndPublish(ConnectionPoolCheckingOutConnectionFailedEvent @event)
        {
            Logger?.LogInformation(@event.Exception,
                Id_Message_Reason,
                Id,
                "Checking out failed with",
                @event.Reason);

            _eventsPublisher?.Publish(EventType.ConnectionPoolCheckingOutConnectionFailed, @event);
        }

        public void LogAndPublish(ConnectionPoolCheckingInConnectionEvent @event)
        {
            Logger?.LogInformation(Id_Message_ConnectionId,
                Id,
                "Checking connection in",
                @event.ConnectionId);

            _eventsPublisher?.Publish(EventType.ConnectionPoolCheckingInConnection, @event);
        }

        public void LogAndPublish(ConnectionPoolCheckedInConnectionEvent @event)
        {
            Logger?.LogInformation(Id_Message_ConnectionId,
                Id,
                "Checked connection in",
                @event.ConnectionId) ;

            _eventsPublisher?.Publish(EventType.ConnectionPoolCheckedInConnection, @event);
        }

        public void LogAndPublish(ConnectionPoolAddingConnectionEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Adding connection");

            _eventsPublisher?.Publish(EventType.ConnectionPoolAddingConnection, @event); ;
        }

        public void LogAndPublish(ConnectionPoolAddedConnectionEvent @event)
        {
            Logger?.LogInformation(Id_Message_ConnectionId,
                Id,
                "Connection added",
                @event.ConnectionId);

            _eventsPublisher?.Publish(EventType.ConnectionPoolAddedConnection, @event);
        }

        public void LogAndPublish(ConnectionPoolOpeningEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Opening");

            _eventsPublisher?.Publish(EventType.ConnectionPoolOpening, @event);
        }

        public void LogAndPublish(ConnectionPoolOpenedEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Opened");

            _eventsPublisher?.Publish(EventType.ConnectionPoolOpened, @event);
        }

        public void LogAndPublish(ConnectionPoolReadyEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Ready");

            _eventsPublisher?.Publish(EventType.ConnectionPoolReady, @event);
        }

        public void LogAndPublish(ConnectionPoolClosingEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Closing");

            _eventsPublisher?.Publish(EventType.ConnectionPoolClosing, @event);
        }

        public void LogAndPublish(ConnectionPoolClosedEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Closed");

            _eventsPublisher?.Publish(EventType.ConnectionPoolClosed, @event);
        }

        public void LogAndPublish(ConnectionPoolClearingEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Clearing");

            _eventsPublisher?.Publish(EventType.ConnectionPoolClearing, @event);
        }

        public void LogAndPublish(ConnectionPoolClearedEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Cleared");

            _eventsPublisher?.Publish(EventType.ConnectionPoolCleared, @event);
        }

        public void LogAndPublish(ConnectionPoolRemovingConnectionEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Removing");

            _eventsPublisher?.Publish(EventType.ConnectionPoolRemovingConnection, @event);
        }

        public void LogAndPublish(ConnectionPoolRemovedConnectionEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Removed");

            _eventsPublisher?.Publish(EventType.ConnectionPoolRemovedConnection, @event);
        }

        public void LogAndPublish(ConnectionCreatedEvent @event)
        {
            Logger?.LogInformation(Id_Message_ConnectionId,
                Id,
                "Connection created",
                @event.ConnectionId);

            _eventsPublisher?.Publish(EventType.ConnectionCreated, @event);
        }

        #endregion

        #region Cluster

        public void LogAndPublish(ClusterDescriptionChangedEvent @event)
        {
            Logger?.LogInformation(Id_Message_Description,
                Id,
                "Description changed",
                @event.NewDescription);

            _eventsPublisher?.Publish(EventType.ClusterDescriptionChanged, @event);
        }

        public void LogAndPublish(ClusterSelectingServerEvent @event)
        {
            Logger?.LogInformation(Id_Message_OperationId,
                Id,
                "Selecting server",
                @event.OperationId);

            _eventsPublisher?.Publish(EventType.ClusterSelectingServer, @event);
        }

        public void LogAndPublish(ClusterSelectedServerEvent @event)
        {
            Logger?.LogInformation(Id_Message_OperationId,
                Id,
                "Selected server",
                @event.OperationId);

            _eventsPublisher?.Publish(EventType.ClusterSelectedServer, @event);
        }

        public void LogAndPublish(ClusterSelectingServerFailedEvent @event)
        {
            Logger?.LogInformation(@event.Exception,
                Id_Message_OperationId,
                Id,
                "Selecting server failed",
                @event.OperationId);

            _eventsPublisher?.Publish(EventType.ClusterSelectingServerFailed, @event);
        }

        public void LogAndPublish(ClusterClosingEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Closing");

            _eventsPublisher?.Publish(EventType.ClusterClosing, @event);
        }

        public void LogAndPublish(ClusterClosedEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Closed");

            _eventsPublisher?.Publish(EventType.ClusterClosed, @event);
        }

        public void LogAndPublish(ClusterOpeningEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Opening");

            _eventsPublisher?.Publish(EventType.ClusterOpening, @event);
        }

        public void LogAndPublish(ClusterOpenedEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Opened");

            _eventsPublisher?.Publish(EventType.ClusterOpened, @event);
        }

        public void LogAndPublish(ClusterAddingServerEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Adding");

            _eventsPublisher?.Publish(EventType.ClusterAddingServer, @event);
        }

        public void LogAndPublish(ClusterAddedServerEvent @event)
        {
            Logger?.LogInformation(Id_Message_ServerId, Id, "Added server", @event.ServerId);

            _eventsPublisher?.Publish(EventType.ClusterAddedServer, @event);
        }

        public void LogAndPublish(ClusterRemovingServerEvent @event)
        {
            Logger?.LogInformation(Id_Message_ServerId, Id, "Removing server", @event.ServerId);

            _eventsPublisher?.Publish(EventType.ClusterRemovingServer, @event);
        }

        public void LogAndPublish(ClusterRemovedServerEvent @event)
        {
            Logger?.LogInformation(Id_Message_ServerId_Reason_Duration,
                Id,
                "Removed server",
                @event.ServerId,
                @event.Reason,
                @event.Duration);

            _eventsPublisher?.Publish(EventType.ClusterRemovedServer, @event);
        }

        public void LogAndPublish(SdamInformationEvent @event)
        {
            Logger?.LogInformation(Id_Message_Information, Id, "SdamInformation", @event.Message);

            try
            {
                _eventsPublisher?.Publish(EventType.SdamInformation, @event);
            }
            catch (Exception publishException)
            {
                Logger?.LogDebug(publishException, "Failed publishing SdamInformationEvent event");
            }
        }

        public void LogAndPublish(Exception ex, SdamInformationEvent @event)
        {
            Logger?.LogInformation(ex, Id_Message_Information, Id, "SdamInformation", @event.Message);

            try
            {
                _eventsPublisher?.Publish(EventType.SdamInformation, @event);
            }
            catch (Exception publishException)
            {
                Logger?.LogDebug(publishException, "Failed publishing SdamInformationEvent event");
            }
        }

        #endregion

        #region SDAM

        public void LogAndPublish(ServerHeartbeatStartedEvent @event)
        {
            Logger?.LogInformation(Id_Message_ConnectionId, Id, "Heartbeat started", @event.ConnectionId);

            _eventsPublisher?.Publish(EventType.ServerHeartbeatStarted, @event);
        }

        public void LogAndPublish(ServerHeartbeatSucceededEvent @event)
        {
            Logger?.LogInformation(Id_Message_ConnectionId, Id, "Heartbeat succeeded", @event.ConnectionId);

            _eventsPublisher?.Publish(EventType.ServerHeartbeatSucceeded, @event);
        }

        public void LogAndPublish(ServerHeartbeatFailedEvent @event)
        {
            Logger?.LogInformation(@event.Exception, Id_Message_ConnectionId, Id, "Heartbeat failed", @event.ConnectionId);

            _eventsPublisher?.Publish(EventType.ServerHeartbeatFailed, @event);
        }

        public void LogAndPublish(SdamInformationEvent @event, Exception ex)
        {
            Logger?.LogInformation(ex, Id_Message_Information, Id, "SdamInformation", @event.Message);

            try
            {
                _eventsPublisher?.Publish(EventType.SdamInformation, @event);
            }
            catch (Exception publishException)
            {
                // Ignore any exceptions thrown by the handler (note: event handlers aren't supposed to throw exceptions)
                // Backward compatibility
                Logger?.LogWarning(publishException, "Failed publishing event {Event}", @event);
            }
        }

        public void LogAndPublish(ServerOpeningEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Opening");

            _eventsPublisher?.Publish(EventType.ServerOpening, @event);
        }

        public void LogAndPublish(ServerOpenedEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Opened");

            _eventsPublisher?.Publish(EventType.ServerOpened, @event);
        }

        public void LogAndPublish(ServerClosingEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Closing");

            _eventsPublisher?.Publish(EventType.ServerClosing, @event);
        }

        public void LogAndPublish(ServerClosedEvent @event)
        {
            Logger?.LogInformation(Id_Message, Id, "Closed");

            _eventsPublisher?.Publish(EventType.ServerClosed, @event);
        }

        public void LogAndPublish(ServerDescriptionChangedEvent @event)
        {
            Logger?.LogInformation(Id_Message_Description, Id, "Description changed", @event.NewDescription);

            _eventsPublisher?.Publish(EventType.ServerDescriptionChanged, @event);
        }

        #endregion
    }
}
