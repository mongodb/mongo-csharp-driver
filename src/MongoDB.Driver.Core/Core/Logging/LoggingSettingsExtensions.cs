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

namespace MongoDB.Driver.Core.Logging
{
    internal static class LoggingSettingsExtensions
    {
        public static ILoggerFactory ToInternalLoggerFactory(this LoggingSettings loggingSettings) =>
            loggingSettings?.LoggerFactory switch
            {
                _ when loggingSettings?.LoggerFactory != null => new LoggerFactoryCategoryDecorator(loggingSettings.LoggerFactory, loggingSettings),
                _ => null
            };

        public static ILogger<TCategory> CreateLogger<TCategory>(this LoggingSettings loggingSettings) =>
            loggingSettings?.LoggerFactory switch
            {
                _ when loggingSettings?.LoggerFactory != null => new LoggerFactoryCategoryDecorator(loggingSettings.LoggerFactory, loggingSettings).CreateLogger<TCategory>(),
                _ => null
            };
    }
}
