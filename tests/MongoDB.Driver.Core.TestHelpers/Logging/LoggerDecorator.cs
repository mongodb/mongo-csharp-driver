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

using System.Runtime.CompilerServices;

namespace MongoDB.Driver.Core.TestHelpers.Logging
{
    internal class LoggerDecorator : ILogger
    {
        private readonly string _decoration;
        private ILogger _loggerBase;

        public LoggerDecorator(ILogger loggerBase, string decoration)
        {
            _loggerBase = loggerBase;
            _decoration = decoration;
        }

        public void Log(LogLevel logLevel, string decoration, string format, params object[] arguments) =>
            _loggerBase.Log(logLevel, Compose(_decoration, decoration), format, arguments);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static string Compose(string baseString, string optionalString)
        {
            var result = optionalString == null ? baseString : baseString + optionalString;

            if (result != null)
            {
                result = result.Replace("{", "{{").Replace("}", "}}");
            }

            return result;
        }
    }

    internal class LoggerDecorator<T> : LoggerDecorator, ILogger<T>
    {
        public LoggerDecorator(ILogger<T> loggerBase, string decoration) :
            base(loggerBase, decoration)
        {
        }
    }

    internal class TypedLoggerDecorator<T> : LoggerDecorator, ILogger<T>
    {
        public TypedLoggerDecorator(ILogger loggerBase, string decoration = null) :
            base(loggerBase, Compose(typeof(T).Name, decoration))
        {
        }
    }
}
