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
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Configuration;

/// <summary>
/// Configures what a <see cref="MongoClient"/> logs and traces.
/// </summary>
public sealed class DiagnosticsBuilder
{
    private readonly List<IEventSubscriber> _subscribers = new();

    internal DiagnosticsBuilder()
    {
        LogMaxDocumentSize = MongoInternalDefaults.Logging.MaxDocumentSize;
        TracingEnabled = true;
    }

    /// <summary>
    /// Gets or sets the logger factory the driver logs to. The default value is <c>null</c>, which disables logging.
    /// </summary>
    [CLSCompliant(false)]
    public ILoggerFactory LoggerFactory
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the maximum length, in characters, of the documents that are written to the log; longer
    /// documents are truncated. Must be greater than or equal to zero. Has no effect unless
    /// <see cref="LoggerFactory"/> is set. The default value is 1000.
    /// </summary>
    public int LogMaxDocumentSize
    {
        get;
        set => field = Ensure.IsGreaterThanOrEqualToZero(value, nameof(LogMaxDocumentSize));
    }

    /// <summary>
    /// Gets or sets whether OpenTelemetry tracing is enabled for this client. Tracing only produces activities
    /// while an OpenTelemetry listener is subscribed to the driver's activity source. Set to <c>false</c> to
    /// disable tracing for this client. The default value is <c>true</c>.
    /// </summary>
    public bool TracingEnabled
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the maximum length of the <c>db.query.text</c> attribute of the tracing activities. Must be
    /// greater than or equal to zero. The default value is 0, which means the query text is not recorded.
    /// </summary>
    public int TracingQueryTextMaxLength
    {
        get;
        set => field = Ensure.IsGreaterThanOrEqualToZero(value, nameof(TracingQueryTextMaxLength));
    }

    /// <summary>
    /// Subscribes a handler to events of type <typeparamref name="TEvent"/>. Subscriptions accumulate:
    /// calling this more than once, including more than once for the same event type, adds handlers
    /// rather than replacing them.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="handler">The handler.</param>
    public DiagnosticsBuilder Subscribe<TEvent>(Action<TEvent> handler)
    {
        Ensure.IsNotNull(handler, nameof(handler));

        _subscribers.Add(new SingleEventSubscriber<TEvent>(handler));
        return this;
    }

    /// <summary>
    /// Subscribes the specified subscriber. Subscriptions accumulate: calling this more than once adds
    /// subscribers rather than replacing them.
    /// </summary>
    /// <param name="subscriber">The subscriber.</param>
    public DiagnosticsBuilder Subscribe(IEventSubscriber subscriber)
    {
        Ensure.IsNotNull(subscriber, nameof(subscriber));

        _subscribers.Add(subscriber);
        return this;
    }

    // null when logging is disabled
    internal LoggingSettings CreateLoggingSettings()
        => LoggerFactory == null ? null : new LoggingSettings(LoggerFactory, LogMaxDocumentSize);

    // null when tracing has its default configuration, which is also what MongoClientSettings uses
    internal TracingOptions CreateTracingOptions()
        => TracingEnabled && TracingQueryTextMaxLength == 0
            ? null
            : new TracingOptions { Disabled = !TracingEnabled, QueryTextMaxLength = TracingQueryTextMaxLength };

    internal IReadOnlyList<IEventSubscriber> GetSubscribers()
        => _subscribers.ToArray();
}
