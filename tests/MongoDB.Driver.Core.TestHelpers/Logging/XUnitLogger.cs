/* Copyright 2010e-present MongoDB Inc.
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
using Microsoft.Extensions.Logging.Abstractions.Internal;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.TestHelpers.Logging
{
    [DebuggerStepThrough]
    internal sealed class XUnitLogger : ILogger
    {
        private readonly string _category;
        private readonly XUnitOutputAccumulator _output;

        public XUnitLogger(string category, XUnitOutputAccumulator output)
        {
            _category = category;
            _output = Ensure.IsNotNull(output, nameof(output));
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (state is not IEnumerable<KeyValuePair<string, object>> keyValuePairs)
            {
                throw new Exception($"Unsupported log state {state}");
            }

            var formatterObject = (Func<object, Exception, string>)(Delegate)formatter;
            _output.Log(logLevel, _category, keyValuePairs, exception, formatterObject);
        }

        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable BeginScope<TState>(TState state) { return NullScope.Instance; }
    }
}
