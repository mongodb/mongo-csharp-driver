﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Diagnostics;
using System.Linq;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.Driver.Core.TestHelpers.Logging
{
    [DebuggerStepThrough]
    public abstract class LoggableTestClass : IDisposable
    {
        public LoggableTestClass(ITestOutputHelper output, bool includeAllCategories = false)
        {
            var logCategoriesToExclude = includeAllCategories ? null : new[]
            {
                "MongoDB.Command",
                "MongoDB.Connection"
            };

            TestOutput = Ensure.IsNotNull(output, nameof(output));
            Accumulator = new XUnitOutputAccumulator(logCategoriesToExclude);
            MinLogLevel = LogLevel.Warning;

            LoggerFactory = new XUnitLoggerFactory(Accumulator).DecorateCategories();
            Logger = LoggerFactory.CreateLogger<LoggableTestClass>();
        }

        private ITestOutputHelper TestOutput { get; }
        private XUnitOutputAccumulator Accumulator { get; }

        protected ILogger<LoggableTestClass> Logger { get; }
        protected ILoggerFactory LoggerFactory { get; }
        protected LogLevel MinLogLevel { get; set; }

        public LogEntry[] Logs => Accumulator.Logs;

        protected ILogger<TCategory> CreateLogger<TCategory>() => LoggerFactory.CreateLogger<TCategory>();

        protected virtual void DisposeInternal() { }

        public void OnException(Exception ex)
        {
            TestOutput.WriteLine("Formatted exception: {0}", FormatException(ex));

            if (ex is TestTimeoutException)
            {
                try
                {
                    LogStackTrace();
                }
                catch
                {
                    // fail silently
                }
            }

            if (MinLogLevel > LogLevel.Debug)
            {
                MinLogLevel = LogLevel.Debug;
            }
        }

        public void Dispose()
        {
            DisposeInternal();

            Flush(MinLogLevel);
        }

        public void Flush(LogLevel? minLogLevel)
        {
            var logs = Logs;
            var minLogLevelActual = minLogLevel ?? LogLevel.Trace;

            foreach (var logEntry in logs)
            {
                if (logEntry.LogLevel >= minLogLevelActual)
                {
                    TestOutput.WriteLine(logEntry.ToString());
                }
            }
        }

        private string FormatException(Exception exception)
        {
            var result = exception.ToString();

            switch (exception)
            {
                case MongoCommandException commandException:
                    result += $"{commandException.ConnectionId} {commandException.Command} {commandException.Result}";
                    break;
                case MongoConnectionException connectionException:
                    result += connectionException.ConnectionId.ToString();
                    break;
                default: break;
            }

            return result;
        }

        private void LogStackTrace()
        {
            var pid = Process.GetCurrentProcess().Id;

            using (var dataTarget = DataTarget.CreateSnapshotAndAttach(pid))
            {
                var runtimeInfo = dataTarget.ClrVersions[0];
                var runtime = runtimeInfo.CreateRuntime();

                TestOutput.WriteLine("Found {0} threads", runtime.Threads.Length);

                foreach (var clrThread in runtime.Threads)
                {
                    var methods = string.Join(",", clrThread
                        .EnumerateStackTrace()
                        .Where(f => f.Method != null)
                        .Take(20)
                        .Select(f => f.Method.Type.Name + "." + f.Method.Name));

                    if (!string.IsNullOrWhiteSpace(methods))
                    {
                        TestOutput.WriteLine("Thread {0} at {1}", clrThread.ManagedThreadId, methods);
                    }
                }
            }
        }
    }
}
