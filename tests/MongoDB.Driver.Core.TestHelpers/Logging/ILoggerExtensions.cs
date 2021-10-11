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

namespace MongoDB.Driver.Core.TestHelpers.Logging
{
    public static class ILoggerExtensions
    {
        public static ILogger<T> Decorate<T>(this ILogger<T> logger, string decoration) =>
            logger != null ? new LoggerDecorator<T>(logger, decoration) : null;

        public static void Error(this ILogger logger, string format, params object[] arguments) =>
            logger?.Log(LogLevel.Error, null, format, arguments);

        public static void Error<T>(this ILogger<T> logger, string message, params object[] arguments) =>
            logger?.Log(LogLevel.Error, null, message, arguments);

        public static void Warning(this ILogger logger, string format, params object[] arguments) =>
            logger?.Log(LogLevel.Warning, null, format, arguments);

        public static void Warning<T>(this ILogger<T> logger, string message, params object[] arguments) =>
            logger?.Log(LogLevel.Warning, null, message, arguments);

        public static void Info(this ILogger logger, string format, params object[] arguments) =>
            logger?.Log(LogLevel.Information, null, format, arguments);

        public static void Info<T>(this ILogger<T> logger, string message, params object[] arguments) =>
            logger?.Log(LogLevel.Information, null, message, arguments);

        public static void Debug(this ILogger logger, string format, params object[] arguments) =>
            logger?.Log(LogLevel.Debug, null, format, arguments);

        public static void Debug<T>(this ILogger<T> logger, string message, params object[] arguments) =>
            logger?.Log(LogLevel.Debug, null, message, arguments);

        public static void Trace(this ILogger logger, string format, params object[] arguments) =>
            logger?.Log(LogLevel.Trace, null, format, arguments);

        public static void Trace<T>(this ILogger<T> logger, string format, params object[] arguments) =>
            logger?.Log(LogLevel.Trace, null, format, arguments);
    }

    internal static class ILoggerFactoryExtensions
    {
        public static ILogger<TCategory> CreateLogger<TCategory>(this ILoggerFactory loggerFactory, string decoration) =>
            loggerFactory?.CreateLogger<TCategory>().Decorate(decoration);
    }
}
