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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Logging
{
    internal sealed class LoggerFactoryCategoryDecorator : ILoggerFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly LoggingSettings _loggingSettings;

        public LoggerFactoryCategoryDecorator(ILoggerFactory loggerFactory, LoggingSettings loggingSettings)
        {
            _loggerFactory = Ensure.IsNotNull(loggerFactory, nameof(loggerFactory));
            _loggingSettings = Ensure.IsNotNull(loggingSettings, nameof(loggingSettings));
        }

        public LoggingSettings LoggingSettings => _loggingSettings;

        public void AddProvider(ILoggerProvider provider) => _loggerFactory.AddProvider(provider);

        public ILogger CreateLogger(string categoryName) =>
            _loggerFactory.CreateLogger(LogCategoryHelper.DecorateCategoryName(categoryName));

        public void Dispose() => _loggerFactory.Dispose();
    }
}
