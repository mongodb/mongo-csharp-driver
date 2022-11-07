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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Logging
{
    public class EventLoggerTests
    {
        [Theory]
        [MemberData(nameof(EventsData))]
        internal void LogAndPublish_should_log_and_publish<TEventCategory, TEvent>(
            TEventCategory eventCategory,
            TEvent @event,
            bool isHandlerRegistered,
            bool isLoggingEnabled)
            where TEventCategory : LogCategories.EventCategory
            where TEvent : struct, IEvent
        {
            object eventCaptured = null;
            Mock<IEventSubscriber> eventSubscriber = new Mock<IEventSubscriber>();
            Action<TEvent> eventHandler = e => eventCaptured = e;
            if (isHandlerRegistered)
            {
                eventSubscriber
                    .Setup(s => s.TryGetEventHandler(out eventHandler))
                    .Returns(true);
            }

            Mock<ILogger<TEventCategory>> logger = null;

            if (isLoggingEnabled)
            {
                logger = new Mock<ILogger<TEventCategory>>();
                logger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
            }

            var eventLogger = new EventLogger<TEventCategory>(eventSubscriber.Object, logger?.Object);
            eventLogger.LogAndPublish(@event);

            eventSubscriber.Verify(s => s.TryGetEventHandler(out eventHandler), Times.Once);

            if (isHandlerRegistered)
            {
                eventCaptured.Should().Be(@event);
            }
            else
            {
                eventCaptured.Should().BeNull();
            }

            if (isLoggingEnabled)
            {
                logger.Verify(l => l.Log(LogLevel.Debug, It.IsAny<EventId>(), It.IsNotNull<object>(), null, It.IsNotNull<Func<object, Exception, string>>()), Times.Once);
            }

            eventLogger.IsEventTracked<TEvent>().Should().Be(isLoggingEnabled || isHandlerRegistered);
        }

        private static IEnumerable<object[]> EventsData()
        {
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            var connectionId = new ConnectionId(serverId, 1);
            var clusterDescription = new ClusterDescription(clusterId, false, default, default, default);

            var eventsData = new (object, object)[]
            {
                (new LogCategories.Command(), new CommandStartedEvent("test", new Bson.BsonDocument(), new DatabaseNamespace("test"), 1, 1, connectionId)),
                (new LogCategories.Connection(), new ConnectionCreatedEvent(connectionId, null, 1)),
                (new LogCategories.SDAM(), new ServerHeartbeatStartedEvent(connectionId, true)),
                (new LogCategories.ServerSelection(), new ClusterSelectingServerEvent(clusterDescription, default, default))
            };

            var booleanValues = new[] { true, false };

            var result = from isHandlerRegistered in booleanValues
                         from isLoggingEnabled in booleanValues
                         from eventData in eventsData
                         select new object[] { eventData.Item1, eventData.Item2, isHandlerRegistered, isLoggingEnabled };

            return result;
        }
    }
}
