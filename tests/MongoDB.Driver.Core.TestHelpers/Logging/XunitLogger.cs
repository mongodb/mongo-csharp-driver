/* Copyright 2021-present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using Xunit.Abstractions;

namespace MongoDB.Driver.Core.TestHelpers.Logging
{
    [DebuggerStepThrough]
    internal sealed class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _output;
        private readonly List<(DateTime, LogLevel, string, string, object[])> _logs;

        public XunitLogger(ITestOutputHelper output)
        {
            _output = Ensure.IsNotNull(output, nameof(output));
            _logs = new List<(DateTime, LogLevel, string, string, object[])>();
        }

        public void Log(LogLevel logLevel, string decorator, string format, params object[] arguments)
        {
            lock (_logs)
            {
                _logs.Add((DateTime.UtcNow, logLevel, decorator, format, arguments));
            }
        }

        public void Flush(LogLevel? minLogLevel)
        {
            (DateTime, LogLevel, string, string, object[])[] logsCloned = null;

            lock (_logs)
            {
                logsCloned = _logs.ToArray();
            }

            var minLogLevelActual = minLogLevel ?? LogLevel.Trace;

            foreach (var (dateTime, logLevel, decorator, format, arguments) in logsCloned)
            {
                if (logLevel >= minLogLevelActual)
                {
                    _output.WriteLine($"{dateTime:hh:mm:ss.FFF)}_{logLevel}<{decorator}> {format}", arguments);
                }
            }
        }
    }
}
