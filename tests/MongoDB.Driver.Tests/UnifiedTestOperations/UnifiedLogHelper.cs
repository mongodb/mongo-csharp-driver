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
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.TestHelpers.Logging;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public static class UnifiedLogHelper
    {
        public static LogEntry[] FilterLogs(
            LogEntry[] logs,
            string clientId,
            int clusterId,
            Dictionary<string, Dictionary<string, LogLevel>> componentsVerbosity,
            BsonArray logsToIgnore,
            Predicate<LogEntry> filter = null)
        {
            var clientConfiguration = componentsVerbosity[clientId];
            var logMessagesToIgnore = ExtractMessagesToIgnore(logsToIgnore);

            var result = logs.Where(l =>
                l.ClusterId == clusterId &&
                filter?.Invoke(l) != false &&
                clientConfiguration.TryGetValue(l.Category, out var logLevel) &&
                l.LogLevel >= logLevel &&
                ShouldNotIgnoreLogMessage(l))
                .ToArray();

            bool ShouldNotIgnoreLogMessage(LogEntry logEntry) =>
                logMessagesToIgnore?.FirstOrDefault(l =>
                    l.LogLevel == logEntry.LogLevel &&
                    l.Category == logEntry.Category &&
                    l.Message == logEntry.Message) == null;

            return result;
        }

        public static string ParseCategory(string category) =>
           category switch
           {
               "client" => LogCategoryHelper.GetCategoryName<LogCategories.Client>(),
               "command" => LogCategoryHelper.GetCategoryName<LogCategories.Command>(),
               "connection" => LogCategoryHelper.GetCategoryName<LogCategories.Connection>(),
               "topology" => LogCategoryHelper.GetCategoryName<LogCategories.SDAM>(),
               "serverSelection" => LogCategoryHelper.GetCategoryName<LogCategories.ServerSelection>(),
               _ => throw new ArgumentOutOfRangeException(nameof(category), category)
           };

        public static LogLevel ParseLogLevel(string logLevel) =>
            logLevel switch
            {
                "error" => LogLevel.Error,
                "warning" => LogLevel.Warning,
                "info" or "notice" => LogLevel.Information,
                "debug" => LogLevel.Debug,
                "trace" => LogLevel.Trace,
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel)
            };


        private static LogEntry[] ExtractMessagesToIgnore(BsonArray logsToIgnore)
        {
            var logMessagesToIgnore = new List<LogEntry>();

            if (logsToIgnore != null)
            {
                foreach (var logToIgnore in logsToIgnore.Cast<BsonDocument>())
                {
                    var category = ParseCategory(logToIgnore["component"].AsString);
                    var logLevel = ParseLogLevel(logToIgnore["level"].AsString);
                    var message =
                        logToIgnore.TryGetValue("data", out var logData) &&
                        logData.AsBsonDocument.TryGetValue("message", out var messageValue) &&
                        messageValue.IsString ? messageValue.AsString : null;

                    logMessagesToIgnore.Add(new LogEntry(logLevel, category, new[] { new KeyValuePair<string, object>("Message", message) }, null, null));
                }
            }

            return logMessagesToIgnore.ToArray();
        }
    }
}
