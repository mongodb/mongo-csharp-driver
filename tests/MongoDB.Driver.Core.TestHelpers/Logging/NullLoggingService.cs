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
using Microsoft.Extensions.Logging.Abstractions;

namespace MongoDB.Driver.Core.TestHelpers.Logging
{
    public sealed class NullLoggingService : ILoggingService
    {
        public static ILoggingService Instance = new NullLoggingService();

        private NullLoggingService() { }

        public ILoggerFactory LoggerFactory { get; } = NullLoggerFactory.Instance;
        public LogEntry[] Logs { get; } = new LogEntry[0];
    }
}
