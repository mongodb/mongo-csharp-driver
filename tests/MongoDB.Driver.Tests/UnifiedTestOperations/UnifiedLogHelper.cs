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
            Predicate<LogEntry> filter = null)
        {
            var clientConfiguration = componentsVerbosity[clientId];

            var result = logs.Where(l =>
                filter?.Invoke(l) != false &&
                l.ClusterId == clusterId &&
                clientConfiguration.TryGetValue(l.Category, out var logLevel) &&
                l.LogLevel >= logLevel)
                .ToArray();

            return result;
        }

        public static string ParseCategory(string category) =>
           category switch
           {
               "command" => LogCategoryHelper.GetCategoryName<LogCategories.Command>(),
               "connection" => LogCategoryHelper.GetCategoryName<LogCategories.Connection>(),
               "sdam" => LogCategoryHelper.GetCategoryName<LogCategories.SDAM>(),
               "serverSelection" => LogCategoryHelper.GetCategoryName<LogCategories.ServerSelection>(),
               _ => throw new ArgumentOutOfRangeException(nameof(category), category)
           };

        public static LogLevel ParseLogLevel(string logLevel) =>
            logLevel switch
            {
                "error" => LogLevel.Error,
                "warning" => LogLevel.Warning,
                "informational" or "notice" => LogLevel.Information,
                "debug" => LogLevel.Debug,
                "trace" => LogLevel.Trace,
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel)
            };
    }
}
