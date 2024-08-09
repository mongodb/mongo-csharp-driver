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
using System.Collections.Concurrent;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication.Oidc
{
    internal interface IOidcCallbackAdapterFactory
    {
        IOidcCallbackAdapter Get(OidcConfiguration configuration);
    }

    internal class OidcCallbackAdapterCachingFactory : IOidcCallbackAdapterFactory
    {
        public static readonly OidcCallbackAdapterCachingFactory Instance = new(SystemClock.Instance, EnvironmentVariableProvider.Instance);

        private readonly IClock _clock;
        private readonly IEnvironmentVariableProvider _environmentVariableProvider;
        private readonly ConcurrentDictionary<OidcConfiguration, IOidcCallbackAdapter> _cache = new();

        public OidcCallbackAdapterCachingFactory(IClock clock, IEnvironmentVariableProvider environmentVariableProvider)
        {
            _clock = clock;
            _environmentVariableProvider = environmentVariableProvider;
        }

        public IOidcCallbackAdapter Get(OidcConfiguration configuration)
            => _cache.GetOrAdd(configuration, CreateCallbackAdapter);

        internal void Reset()
            => _cache.Clear();

        private IOidcCallbackAdapter CreateCallbackAdapter(OidcConfiguration configuration)
        {
            var callback = configuration.Callback;

            if (!string.IsNullOrEmpty(configuration.Environment))
            {
                callback = configuration.Environment switch
                {
                    "azure" => new AzureOidcCallback(configuration.TokenResource),
                    "gcp" => new GcpOidcCallback(configuration.TokenResource),
                    "test" => FileOidcCallback.CreateFromEnvironmentVariable("OIDC_TOKEN_FILE", _environmentVariableProvider),
                    _ => throw new NotSupportedException($"Non supported {OidcConfiguration.EnvironmentMechanismPropertyName} value: {configuration.Environment}")
                };
            }

            return new OidcCallbackAdapter(callback, _clock);
        }
    }
}
