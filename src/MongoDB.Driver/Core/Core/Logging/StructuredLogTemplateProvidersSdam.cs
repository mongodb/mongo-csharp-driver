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
        private static string[] __sdamCommonParams = new[]
        {
            TopologyId,
            DriverConnectionId,
            ServerHost,
            ServerPort,
            ServerConnectionId,
            Message,
        };

        private static string SdamCommonParams(params string[] @params) => Concat(__sdamCommonParams, @params);

        private static void AddSdamTemplates()
        {
            AddTemplateProvider<ServerHeartbeatStartedEvent>(
                LogLevel.Debug,
                SdamCommonParams(Awaited),
                (e, _) => GetParams(e.ConnectionId, "Server heartbeat started", e.Awaited));

            AddTemplateProvider<ServerHeartbeatSucceededEvent>(
                LogLevel.Debug,
                SdamCommonParams(Awaited, DurationMS, Reply),
                (e, o) => GetParams(e.ConnectionId, "Server heartbeat succeeded", e.Awaited, (long)e.Duration.TotalMilliseconds, DocumentToString(e.Reply, o)));

            AddTemplateProvider<ServerHeartbeatFailedEvent>(
                LogLevel.Debug,
                SdamCommonParams(Awaited, DurationMS, Failure),
                (e, o) => GetParams(e.ConnectionId, "Server heartbeat failed", e.Awaited, (long)e.Duration.TotalMilliseconds, FormatCommandException(e.Exception, o)));

            AddTemplateProvider<SdamInformationEvent>(
                LogLevel.Debug,
                Concat(new[] { Message }),
                (e, _) => new[] { e.Message });

            AddTemplateProvider<ServerOpeningEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Starting server monitoring"));

            AddTemplateProvider<ServerOpenedEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Started server monitoring"));

            AddTemplateProvider<ServerClosingEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Stopping server monitoring"));

            AddTemplateProvider<ServerClosedEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Stopped server monitoring"));

            AddTemplateProvider<ServerDescriptionChangedEvent>(
                LogLevel.Trace,
                CmapCommonParams(Description),
                (e, _) => GetParams(e.ServerId, "Server description changed", e.NewDescription));
        } 
    }
}
