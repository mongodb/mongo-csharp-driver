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
            ClusterId,
            DriverConnectionId,
            ServerHost,
            ServerPort,
            Message,
        };

        private static string SdamCommonParams(params string[] @params) => Concat(__sdamCommonParams, @params);

        private static void AddSdamTemplates()
        {
            AddTemplateProvider<ServerHeartbeatStartedEvent>(
                LogLevel.Debug,
                SdamCommonParams(),
                (e, _) => GetParams(e.ConnectionId, "Heartbeat started"));

            AddTemplateProvider<ServerHeartbeatSucceededEvent>(
                LogLevel.Debug,
                SdamCommonParams(),
                (e, _) => GetParams(e.ConnectionId, "Heartbeat succeeded"));

            AddTemplateProvider<ServerHeartbeatFailedEvent>(
                LogLevel.Debug,
                SdamCommonParams(),
                (e, _) => GetParams(e.ConnectionId, "Heartbeat failed"));

            AddTemplateProvider<SdamInformationEvent>(
                LogLevel.Debug,
                Concat(new[] { Message }),
                (e, _) => new[] { e.Message });

            AddTemplateProvider<ServerOpeningEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Server opening"));

            AddTemplateProvider<ServerOpenedEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Server opened"));

            AddTemplateProvider<ServerClosingEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Server closing"));

            AddTemplateProvider<ServerClosedEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Server closed"));

            AddTemplateProvider<ServerDescriptionChangedEvent>(
                LogLevel.Debug,
                CmapCommonParams(Description),
                (e, _) => GetParams(e.ServerId, "Description changed", e.NewDescription));
        }
    }
}
