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
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using static MongoDB.Driver.Core.Logging.StructuredLogTemplateProviders;

namespace MongoDB.Driver.Core.Logging
{
    internal static class LoggerExtensions
    {
        public static EventLogger<T> ToEventLogger<T>(this ILogger<T> logger, IEventSubscriber eventSubscriber)
            where T : LogCategories.EventCategory =>
            new EventLogger<T>(eventSubscriber, logger);

        public static EventLogger<T> ToEventLogger<T>(this IEventSubscriber eventSubscriber)
            where T : LogCategories.EventCategory =>
            new EventLogger<T>(eventSubscriber, null);

        public static void LogDebug<T>(this ILogger<T> logger, ClusterId clusterId, string message)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(TopologyId_Message, GetParams(clusterId, message));
            }
        }

        public static void LogTrace<T>(this ILogger<T> logger, ClusterId clusterId, string message)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(TopologyId_Message, GetParams(clusterId, message));
            }
        }

        public static void LogDebug<T>(this ILogger<T> logger, string format, ClusterId clusterId, string message, object arg1)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(format, GetParams(clusterId, message, arg1));
            }
        }

        public static void LogTrace<T>(this ILogger<T> logger, string format, ClusterId clusterId, string message, object arg1)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(format, GetParams(clusterId, message, arg1));
            }
        }

        public static void LogDebug<T>(this ILogger<T> logger, ServerId serverId, string message)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(ServerId_Message, GetParams(serverId, message));
            }
        }

        public static void LogDebug<T>(this ILogger<T> logger, string format, ServerId serverId, string message, object arg1)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(format, GetParams(serverId, message, arg1));
            }
        }
    }
}
