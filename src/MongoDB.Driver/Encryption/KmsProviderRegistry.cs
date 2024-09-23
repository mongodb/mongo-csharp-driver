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

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// Kms Provider Registry.
    /// </summary>
    public sealed class KmsProviderRegistry
    {
        /// <summary>
        /// Kms Provider Registry Instance.
        /// </summary>
        public static readonly KmsProviderRegistry Instance = CreateDefaultInstance();

        private static KmsProviderRegistry CreateDefaultInstance()
        {
            var registry =  new KmsProviderRegistry();
            registry.Register(GcpKmsProvider.ProviderName, () => GcpKmsProvider.Instance);
            registry.Register(AzureKmsProvider.ProviderName, () => AzureKmsProvider.Instance);
            return registry;
        }

        private readonly ConcurrentDictionary<string, Func<IKmsProvider>> _registry = new();

        internal KmsProviderRegistry()
        {
        }

        /// <summary>
        /// Registers new Kms Provider.
        /// </summary>
        /// <param name="kmsProviderName">Kms Provider Name.</param>
        /// <param name="factory">Factory method.</param>
        public void Register(string kmsProviderName, Func<IKmsProvider> factory)
        {
            Ensure.IsNotNullOrEmpty(kmsProviderName, nameof(kmsProviderName));
            Ensure.IsNotNull(factory, nameof(factory));

            if (!_registry.TryAdd(kmsProviderName, factory))
            {
                throw new ArgumentException($"Kms Provider named '{kmsProviderName}' already registered.", nameof(kmsProviderName));
            }
        }

        /// <summary>
        /// Creates KMS provider if possible.
        /// </summary>
        /// <param name="providerName">The requested provider name.</param>
        /// <param name="provider">When this method succeeds contains the created provider, otherwise <value>null</value>.</param>
        /// <returns><value>true</value> if the requested provider was created, otherwise <value>false</value>.</returns>
        public bool TryCreate(string providerName, out IKmsProvider provider)
        {
            Ensure.IsNotNullOrEmpty(providerName, nameof(providerName));

            if (_registry.TryGetValue(providerName, out var factory))
            {
                provider = factory();
                return true;
            }

            provider = null;
            return false;
        }
    }
}
