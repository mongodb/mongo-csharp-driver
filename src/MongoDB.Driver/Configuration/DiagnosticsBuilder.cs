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
    }

    /// <summary>
    /// Gets or sets the logging settings. The default value is <c>null</c>, which disables logging.
    /// </summary>
    public LoggingSettings Logging { get; set; }

    /// <summary>
    /// Gets or sets the tracing options for OpenTelemetry instrumentation. The default value is
    /// <c>null</c>, which uses the default options: tracing is enabled whenever an OpenTelemetry listener is
    /// subscribed to the driver's activity source, and the query text is not recorded. Set
    /// <see cref="TracingOptions.Disabled"/> to <c>true</c> to disable tracing for this client.
    /// </summary>
    public TracingOptions Tracing { get; set; }

    /// <summary>
    /// Subscribes a handler to events of type <typeparamref name="TEvent"/>. Subscriptions accumulate:
    /// calling this more than once, including more than once for the same event type, adds handlers
    /// rather than replacing them.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="handler">The handler.</param>
    /// <returns>The same <see cref="DiagnosticsBuilder"/> instance so that calls can be chained.</returns>
    /// <remarks>
    /// Command events carry the full command and reply, apart from a closed allowlist of
    /// authentication-bearing commands whose bodies the driver redacts. Bulk operations can therefore
    /// deliver multi-megabyte payloads; filter or truncate in the handler.
    /// </remarks>
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
    /// <returns>The same <see cref="DiagnosticsBuilder"/> instance so that calls can be chained.</returns>
    /// <remarks>
    /// Command events carry the full command and reply, apart from a closed allowlist of
    /// authentication-bearing commands whose bodies the driver redacts. Bulk operations can therefore
    /// deliver multi-megabyte payloads; filter or truncate in the subscriber.
    /// </remarks>
    public DiagnosticsBuilder Subscribe(IEventSubscriber subscriber)
    {
        Ensure.IsNotNull(subscriber, nameof(subscriber));

        _subscribers.Add(subscriber);
        return this;
    }
}
