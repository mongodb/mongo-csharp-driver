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

using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core.Logging
{
    internal static partial class StructuredLogsTemplates
    {
        private static string[] __commandCommonParams = new[]
        {
            ClusterId,
            DriverConnectionId,
            ServerHost,
            ServerPort,
            ServerConnectionId,
            RequestId,
            OperationId,
            Message,
            CommandName
        };

        private static string[] CommandCommonParams(params string[] @params) => new[]
        {
            Concat(__commandCommonParams, @params),
            Concat(__commandCommonParams, @params.Concat(new[] { ServiceId }).ToArray())
        };

        private static void AddCommandTemplates()
        {
            AddTemplateProvider<CommandStartedEvent>(
                LogLevel.Debug,
                CommandCommonParams(DatabaseName, Command),
                e => GetParamsOmitNull(
                    e.ConnectionId,
                    e.ConnectionId.ServerValue,
                    e.RequestId,
                    e.OperationId,
                    "Command started",
                    e.CommandName,
                    e.DatabaseNamespace.DatabaseName,
                    e.Command?.ToString(),
                    ommitableParam: e.ServiceId),
                (e, s) => e.ServiceId == null ? s.Templates[0] : s.Templates[1]);

            AddTemplateProvider<CommandSucceededEvent>(
                LogLevel.Debug,
                CommandCommonParams(DurationMS, Reply),
                e => GetParamsOmitNull(
                    e.ConnectionId,
                    e.ConnectionId.ServerValue,
                    e.RequestId,
                    e.OperationId,
                    "Command succeeded",
                    e.CommandName,
                    e.Duration.TotalMilliseconds,
                    e.Reply?.ToString(),
                    ommitableParam: e.ServiceId),
                (e, s) => e.ServiceId == null ? s.Templates[0] : s.Templates[1]);

            AddTemplateProvider<CommandFailedEvent>(
                LogLevel.Debug,
                CommandCommonParams(DurationMS, Failure),
                e => GetParamsOmitNull(
                    e.ConnectionId,
                    e.ConnectionId.ServerValue,
                    e.RequestId,
                    e.OperationId,
                    "Command failed",
                    e.CommandName,
                    e.Duration.TotalMilliseconds,
                    e.Failure?.ToString(),
                    ommitableParam: e.ServiceId),
                (e, s) => e.ServiceId == null ? s.Templates[0] : s.Templates[1]);
        }
    }
}
