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
    public sealed class LogEntry
    {
        private readonly Lazy<string> _message;

        public DateTime Timestamp { get;  }
        public LogLevel LogLevel { get; }
        public string Category { get; }
        public int ClusterId { get; }
        public IEnumerable<KeyValuePair<string, object>> State { get; }
        public Exception Exception { get; }
        public Func<object, Exception, string> Formatter { get; }

        public string FormattedMessage => _message.Value;

        public LogEntry(LogLevel logLevel,
            string category,
            IEnumerable<KeyValuePair<string, object>> state,
            Exception exception,
            Func<object, Exception, string> formatter)
        {
            Timestamp = DateTime.UtcNow;
            LogLevel = logLevel;
            Category = category;
            State = state;
            Exception = exception;
            Formatter = formatter;
            _message = new Lazy<string>(() => Formatter(State, Exception));

            ClusterId = GetParameter("ClusterId") is int clusterId ? clusterId : -1;
        }

        public object GetParameter(string key) =>
            State.FirstOrDefault(s => s.Key == key).Value;

        public T GetParameter<T>(string key) where T : class =>
            GetParameter(key) as T;

        public override string ToString() =>
            $"{Timestamp.ToString("hh:mm:ss.FFFFFFF")}_{LogLevel}<{Category}> {FormattedMessage}";
    }
}
