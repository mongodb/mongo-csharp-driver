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

namespace MongoDB.Driver.Core.TestHelpers.Logging
{
    internal sealed class XUnitOutputAccumulator
    {
        private readonly HashSet<string> _categoriesToExclude;
        private readonly List<LogEntry> _logs;

        public XUnitOutputAccumulator(string[] logCategoriesToExclude)
        {
            _categoriesToExclude = logCategoriesToExclude?.Any() == true ? new HashSet<string>(logCategoriesToExclude) : null;
            _logs = new List<LogEntry>();
        }

        public string[] CatergoriesToExclude => _categoriesToExclude?.ToArray();

        public LogEntry[] Logs
        {
            get
            {
                LogEntry[] logsCloned = null;

                lock (_logs)
                {
                    logsCloned = _logs.ToArray();
                }

                return logsCloned;
            }
        }

        public void Log(LogLevel logLevel,
            string category,
            IEnumerable<KeyValuePair<string, object>> state,
            Exception exception,
            Func<object, Exception, string> formatter)
        {
            if (logLevel <= LogLevel.Warning && _categoriesToExclude?.Contains(category) == true)
            {
                return;
            }

            lock (_logs)
            {
                _logs.Add(new LogEntry(logLevel, category, state, exception, formatter));
            }
        }
    }
}
