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
using Microsoft.Extensions.Logging;

namespace MongoDB.Driver.Core.Logging
{
    internal class LoggerDecorator<T>
    {
        public ILogger<T> Logger { get; }
        public string DecorationName { get; }
        public string DecorationValue { get; }

        public LoggerDecorator(ILogger<T> logger, string decorationName, string decorationValue)
        {
            Logger = logger;
            DecorationName = decorationName;
            DecorationValue = decorationValue;
        }

        public void LogDebug(string message)
        {
            Logger?.LogDebug($"{{{DecorationName}}} {message}", DecorationValue);
        }

        public void LogDebug(Exception exception, string message)
        {
            Logger?.LogDebug(exception, $"{{{DecorationName}}} {message}", DecorationValue);
        }

        public void LogDebug(Exception exception, string message, object arg1, object arg2)
        {
            Logger?.LogDebug(exception, $"{{{DecorationName}}} {message}", DecorationValue, arg1, arg2);
        }

        public void LogDebug(string message, object arg)
        {
            Logger?.LogDebug($"{{{DecorationName}}} {message}", DecorationValue, arg);
        }

        public void LogInformation(string message)
        {
            Logger?.LogInformation($"{{{DecorationName}}} {message}", DecorationValue);
        }

        public void LogWarning(string message)
        {
            Logger?.LogWarning($"{{{DecorationName}}} {message}", DecorationValue);
        }
    }
}
