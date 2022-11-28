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
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core.Logging
{
    internal static partial class StructuredLogTemplateProviders
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
                (e, o) => GetParamsOmitNull(
                    e.ConnectionId,
                    e.ConnectionId.LongServerValue,
                    e.RequestId,
                    e.OperationId,
                    "Command started",
                    e.CommandName,
                    e.DatabaseNamespace.DatabaseName,
                    CommandDocumentToString(e.Command, o),
                    ommitableParam: e.ServiceId),
                (e, s) => e.ServiceId == null ? s.Templates[0] : s.Templates[1]);

            AddTemplateProvider<CommandSucceededEvent>(
                LogLevel.Debug,
                CommandCommonParams(DurationMS, Reply),
                (e, o) => GetParamsOmitNull(
                    e.ConnectionId,
                    e.ConnectionId.LongServerValue,
                    e.RequestId,
                    e.OperationId,
                    "Command succeeded",
                    e.CommandName,
                    e.Duration.TotalMilliseconds,
                    CommandDocumentToString(e.Reply, o),
                    ommitableParam: e.ServiceId),
                (e, s) => e.ServiceId == null ? s.Templates[0] : s.Templates[1]);

            AddTemplateProvider<CommandFailedEvent>(
                LogLevel.Debug,
                CommandCommonParams(DurationMS, Failure),
                (e, o) => GetParamsOmitNull(
                    e.ConnectionId,
                    e.ConnectionId.LongServerValue,
                    e.RequestId,
                    e.OperationId,
                    "Command failed",
                    e.CommandName,
                    e.Duration.TotalMilliseconds,
                    FormatCommandException(e.Failure, o),
                    ommitableParam: e.ServiceId),
                (e, s) => e.ServiceId == null ? s.Templates[0] : s.Templates[1]);
        }

        private static string CommandDocumentToString(BsonDocument document, EventLogFormattingOptions eventLogFormattingOptions)
        {
            if (document == null)
            {
                return null;
            }

            return TruncateIfNeeded(document.ToString(), eventLogFormattingOptions.MaxDocumentSize);
        }

        private static string FormatCommandException(Exception exception, EventLogFormattingOptions eventLogFormattingOptions)
        {
            if (exception == null)
            {
                return null;
            }

            var serverResult = (exception as MongoCommandException)?.Result;
            var result = serverResult != null ? $"{exception} server reply: {serverResult}" : exception.ToString();

            return TruncateIfNeeded(result, eventLogFormattingOptions.MaxDocumentSize);
        }
    }
}
