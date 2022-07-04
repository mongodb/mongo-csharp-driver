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

using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Logging
{
    internal static class LoggerExtentions
    {
        public static LoggerDecorator<T> Decorate<T>(this ILogger<T> logger, string id) =>
            new LoggerDecorator<T>(logger, "Id", id);

        public static LoggerDecorator<T> Decorate<T>(this ILogger<T> logger, ServerId serverId) =>
            Decorate<T>(logger, LoggerIdFormatter.FormatId(serverId));

        public static EventsLogger<T> ToEventsLogger<T>(this ILogger<T> logger, IEventSubscriber eventSubscriber, string id)
             where T : LogCategories.EventCategory =>
            new EventsLogger<T>(eventSubscriber, logger, id);

        public static EventsLogger<T> ToEventsLogger<T>(this ILogger<T> logger, IEventSubscriber eventSubscriber, ConnectionId connectionId)
            where T : LogCategories.EventCategory =>
            ToEventsLogger<T>(logger, eventSubscriber, LoggerIdFormatter.FormatId(connectionId));

        public static EventsLogger<T> ToEventsLogger<T>(this ILogger<T> logger, IEventSubscriber eventSubscriber, ServerId serverId)
            where T : LogCategories.EventCategory =>
            ToEventsLogger<T>(logger, eventSubscriber, LoggerIdFormatter.FormatId(serverId));
    }
}
