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
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MongoDB.Driver.SmokeTests.Sdk
{
    internal sealed class LogsTraceListener : TraceListener
    {
        private readonly List<LogEntry> _logEntries = new();
        private readonly object _syncRoot = new();

        private string _currentCategory;
        private LogLevel _currentLogLevel;

        public LogEntry[] GetLogs()
        {
            lock (_syncRoot)
            {
                return _logEntries.ToArray();
            }
        }

        public override void Write(string message)
        {
            // TraceListener delivers the log message in two parts, for example:
            // Log message "MongoDB.Internal.IServerMonitor Verbose: 0 : 1_localhost:27017 Initializing" is split to
            // Write method: "MongoDB.Internal.IServerMonitor Verbose: 0 :"
            // WriteLine method : "1_localhost:27017 Initializing"
            var parts = message.Split(' ');
            var sourceLevel = Enum.Parse(typeof(SourceLevels), parts[1].Trim(':'));

            _currentCategory = parts[0];
            _currentLogLevel = sourceLevel switch
            {
                SourceLevels.All or
                SourceLevels.ActivityTracing => LogLevel.Trace,
                SourceLevels.Verbose => LogLevel.Debug,
                SourceLevels.Information => LogLevel.Information,
                SourceLevels.Warning => LogLevel.Warning,
                SourceLevels.Error => LogLevel.Error,
                SourceLevels.Critical => LogLevel.Critical,
                SourceLevels.Off => LogLevel.None,
                _ => LogLevel.Trace
            };
        }

        public override void WriteLine(string message)
        {
            lock (_syncRoot)
            {
                _logEntries.Add(new LogEntry(_currentLogLevel, _currentCategory, message));
            }
        }
    }
}
