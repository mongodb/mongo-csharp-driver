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
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core.Logging
{
    internal static partial class StructuredLogTemplateProviders
    {
        private static string[] __connectionCommonParams = new[]
        {
            TopologyId,
            DriverConnectionId,
            ServerHost,
            ServerPort,
            ServerConnectionId,
            Message,
        };

        private static string ConnectionCommonParams(params string[] @params) => Concat(__connectionCommonParams, @params);

        private static void AddConnectionTemplates()
        {
            AddTemplateProvider<ConnectionPoolAddedConnectionEvent>(
                LogLevel.Debug,
                ConnectionCommonParams(),
                (e, _) => GetParams(e.ConnectionId, "Connection added"));

            AddTemplateProvider<ConnectionOpenedEvent>(
                 LogLevel.Debug,
                 ConnectionCommonParams(DurationMS),
                 (e, _) => GetParams(e.ConnectionId, "Connection ready", e.Duration.TotalMilliseconds));

            AddTemplateProvider<ConnectionOpeningEvent>(
                LogLevel.Debug,
                ConnectionCommonParams(),
                (e, _) => GetParams(e.ConnectionId, "Connection opening"));

            AddTemplateProvider<ConnectionOpeningFailedEvent>(
                 LogLevel.Debug,
                 ConnectionCommonParams(Reason),
                 (e, _) => GetParams(e.ConnectionId, "Connection opening failed", e.Exception?.ToString()));

            AddTemplateProvider<ConnectionCreatedEvent>(
                LogLevel.Debug,
                ConnectionCommonParams(),
                (e, _) => GetParams(e.ConnectionId, "Connection created"));

            AddTemplateProvider<ConnectionFailedEvent>(
                LogLevel.Debug,
                ConnectionCommonParams(Reason),
                (e, _) => GetParams(e.ConnectionId, "Connection failed", e.Exception?.ToString()));

            AddTemplateProvider<ConnectionClosingEvent>(
                 LogLevel.Debug,
                 ConnectionCommonParams(Reason),
                 (e, _) => GetParams(e.ConnectionId, "Connection closing", "Unknown"));

            AddTemplateProvider<ConnectionClosedEvent>(
                LogLevel.Debug,
                ConnectionCommonParams(Reason),
                (e, _) => GetParams(e.ConnectionId, "Connection closed", "Unknown"));

            AddTemplateProvider<ConnectionReceivedMessageEvent>(
                LogLevel.Trace,
                ConnectionCommonParams(),
                (e, _) => GetParams(e.ConnectionId, "Received"));

            AddTemplateProvider<ConnectionReceivingMessageEvent>(
                 LogLevel.Trace,
                 ConnectionCommonParams(),
                 (e, _) => GetParams(e.ConnectionId, "Receiving"));

            AddTemplateProvider<ConnectionReceivingMessageFailedEvent>(
                 LogLevel.Trace,
                 ConnectionCommonParams(Reason),
                 (e, _) => GetParams(e.ConnectionId, "Receiving failed", e.Exception?.ToString()));

            AddTemplateProvider<ConnectionSendingMessagesEvent>(
                 LogLevel.Trace,
                 ConnectionCommonParams(),
                 (e, _) => GetParams(e.ConnectionId, "Sending"));

            AddTemplateProvider<ConnectionSendingMessagesFailedEvent>(
                 LogLevel.Trace,
                 ConnectionCommonParams(Reason),
                 (e, _) => GetParams(e.ConnectionId, "Sending failed", e.Exception?.ToString()));

            AddTemplateProvider<ConnectionSentMessagesEvent>(
                 LogLevel.Trace,
                 ConnectionCommonParams(),
                 (e, _) => GetParams(e.ConnectionId, "Sent"));
        }
    }
}
