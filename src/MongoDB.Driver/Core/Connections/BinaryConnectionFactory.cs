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
using System.Net;
using System.Threading;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Connections
{
    internal sealed class BinaryConnectionFactory : IConnectionFactory
    {
        // fields
        private readonly IConnectionInitializer _connectionInitializer;
        private readonly IEventSubscriber _eventSubscriber;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConnectionSettings _settings;
        private readonly IStreamFactory _streamFactory;
        private readonly TracingOptions _tracingOptions;
        // TODO: CSOT: temporary here, remove on the next major release, together with socketTimeout
        private readonly TimeSpan _socketReadTimeout;
        private readonly TimeSpan _socketWriteTimeout;

        // constructors
        public BinaryConnectionFactory(
            ConnectionSettings settings,
            IStreamFactory streamFactory,
            IEventSubscriber eventSubscriber,
            ServerApi serverApi,
            ILoggerFactory loggerFactory,
            TracingOptions tracingOptions,
            TimeSpan? socketReadTimeout,
            TimeSpan? socketWriteTimeout)
        {
            _settings = Ensure.IsNotNull(settings, nameof(settings));
            _streamFactory = Ensure.IsNotNull(streamFactory, nameof(streamFactory));
            _eventSubscriber = Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));
            _connectionInitializer = new ConnectionInitializer(settings.ApplicationName, settings.Compressors, serverApi, settings.LibraryInfo);
            _loggerFactory = loggerFactory;
            _tracingOptions = tracingOptions;
            _socketReadTimeout = socketReadTimeout.HasValue && socketReadTimeout > TimeSpan.Zero ? socketReadTimeout.Value : Timeout.InfiniteTimeSpan;
            _socketWriteTimeout = socketWriteTimeout.HasValue && socketWriteTimeout > TimeSpan.Zero ? socketWriteTimeout.Value : Timeout.InfiniteTimeSpan;
        }

        // properties
        public ConnectionSettings ConnectionSettings => _settings;

        // methods
        public IConnection CreateConnection(ServerId serverId, EndPoint endPoint)
        {
            Ensure.IsNotNull(serverId, nameof(serverId));
            Ensure.IsNotNull(endPoint, nameof(endPoint));
            return new BinaryConnection(serverId,
                endPoint,
                _settings,
                _streamFactory,
                _connectionInitializer,
                _eventSubscriber,
                _loggerFactory,
                _tracingOptions,
                _socketReadTimeout,
                _socketWriteTimeout);
        }
    }
}
