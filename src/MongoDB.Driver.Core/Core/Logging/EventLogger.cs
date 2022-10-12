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
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core.Logging
{
    internal sealed class EventLogger<T> where T : LogCategories.EventCategory
    {
        private readonly EventPublisher _eventPublisher;
        private readonly ILogger<T> _logger;
        private readonly EventLogFormattingOptions _eventLogFormattingOptions;

        public static EventLogger<T> Empty { get; } = new EventLogger<T>(null, null);

        public EventLogger(IEventSubscriber eventSubscriber, ILogger<T> logger, EventLogFormattingOptions eventLogFormattingOptions = null)
        {
            _logger = logger;
            _eventPublisher = eventSubscriber != null ? new EventPublisher(eventSubscriber) : null;
            _eventLogFormattingOptions = eventLogFormattingOptions ?? new EventLogFormattingOptions(0);
        }

        public ILogger<T> Logger => _logger;

        public bool IsEventTracked<TEvent>() where TEvent : struct, IEvent =>
            Logger?.IsEnabled(GetEventVerbosity<TEvent>()) == true ||
            _eventPublisher?.IsEventTracked<TEvent>() == true;

        private LogLevel GetEventVerbosity<TEvent>() where TEvent : struct, IEvent =>
            StructuredLogTemplateProviders.GetTemplateProvider(new TEvent().Type).LogLevel;

        public void LogAndPublish<TEvent>(TEvent @event, bool skipLogging = false) where TEvent : struct, IEvent
          => LogAndPublish(null, @event, skipLogging);

        public void LogAndPublish<TEvent>(Exception exception, TEvent @event, bool skipLogging = false) where TEvent : struct, IEvent
        {
            if (!skipLogging)
            {
                var eventTemplateProvider = StructuredLogTemplateProviders.GetTemplateProvider(@event.Type);

                if (_logger?.IsEnabled(eventTemplateProvider.LogLevel) == true)
                {
                    var @params = eventTemplateProvider.GetParams(@event, _eventLogFormattingOptions);
                    var template = eventTemplateProvider.GetTemplate(@event);

                    Log(eventTemplateProvider.LogLevel, template, exception, @params);
                }
            }

            _eventPublisher?.Publish(@event);
        }

        public void LogAndPublish<TEvent, TArg>(TEvent @event, TArg arg) where TEvent : struct, IEvent
        {
            var eventTemplateProvider = StructuredLogTemplateProviders.GetTemplateProvider(@event.Type);

            if (_logger?.IsEnabled(eventTemplateProvider.LogLevel) == true)
            {
                var @params = eventTemplateProvider.GetParams(@event, _eventLogFormattingOptions, arg);
                var template = eventTemplateProvider.GetTemplate(@event);

                Log(eventTemplateProvider.LogLevel, template, exception: null, @params);
            }

            _eventPublisher?.Publish(@event);
        }

        private void Log(LogLevel logLevel, string template, Exception exception, object[] @params)
        {
            switch (logLevel)
            {
                case LogLevel.Trace: _logger.LogTrace(exception, template, @params); break;
                case LogLevel.Debug: _logger.LogDebug(exception, template, @params); break;
                case LogLevel.Information: _logger.LogInformation(exception, template, @params); break;
                case LogLevel.Warning: _logger.LogWarning(exception, template, @params); break;
                case LogLevel.Error: _logger.LogError(exception, template, @params); break;
                case LogLevel.Critical: _logger.LogCritical(exception, template, @params); break;
                default: throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, "Unsupported log level.");
            }
        }
    }
}
