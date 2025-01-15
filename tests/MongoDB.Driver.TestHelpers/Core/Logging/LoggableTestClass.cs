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
using System.Diagnostics;
using System.Linq;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Extensions.Logging;
using MongoDB.TestHelpers.XunitExtensions.TimeoutEnforcing;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.Driver.Core.TestHelpers.Logging
{
    [DebuggerStepThrough]
    public abstract class LoggableTestClass : IDisposable, ILoggingService, ITestExceptionHandler
    {
        private static readonly string[] __defaultCategoriesToExclude = ["MongoDB.Command", "MongoDB.Connection"];

        public LoggableTestClass(ITestOutputHelper output, bool includeAllCategories = false)
            : this (output, new XUnitOutputAccumulator(includeAllCategories ? null : __defaultCategoriesToExclude))
        {}

        internal LoggableTestClass(ITestOutputHelper output, XUnitOutputAccumulator logAccumulator)
        {
            TestOutput = Ensure.IsNotNull(output, nameof(output));
            Accumulator = logAccumulator;
            MinLogLevel = LogLevel.Warning;

            LoggingSettings = new LoggingSettings(new XUnitLoggerFactory(Accumulator), 10000); // Spec test require larger truncation default
            LoggerFactory = LoggingSettings.ToInternalLoggerFactory();
            Logger = LoggerFactory.CreateLogger<LoggableTestClass>();
        }

        private ITestOutputHelper TestOutput { get; }
        private XUnitOutputAccumulator Accumulator { get; }

        protected ILogger<LoggableTestClass> Logger { get; }
        protected LogLevel MinLogLevel { get; set; }

        public ILoggerFactory LoggerFactory { get; }
        public LoggingSettings LoggingSettings { get; }
        public LogEntry[] Logs => Accumulator.Logs;

        protected ILogger<TCategory> CreateLogger<TCategory>() => LoggerFactory.CreateLogger<TCategory>();
        private protected EventLogger<TCategory> CreateEventLogger<TCategory>(IEventSubscriber eventSubscriber) where TCategory : LogCategories.EventCategory =>
            LoggerFactory.CreateEventLogger<TCategory>(eventSubscriber);

        protected virtual void DisposeInternal() { }

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

        public void HandleException(Exception ex)
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
