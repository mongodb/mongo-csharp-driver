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

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// AutoEncryption Provider Registry.
    /// </summary>
    internal sealed class AutoEncryptionProviderRegistry : IAutoEncryptionProviderRegistry
    {
        internal static AutoEncryptionProviderRegistry CreateDefaultInstance() => new AutoEncryptionProviderRegistry();

        private Func<IMongoClient, AutoEncryptionOptions, IAutoEncryptionLibMongoCryptController>  _autoCryptClientControllerFactory;

        public void Register(Func<IMongoClient, AutoEncryptionOptions, IAutoEncryptionLibMongoCryptController> factory)
        {
            // This allows Register to be called more than once as long as the exact same reference is passed
            // each time. This removes a significant synchronization burden from the caller when multiple services
            // may all need to initialize the registry.
            // It is the responsibility of the caller to ensure that the same reference is used for multiple calls.
            if (ReferenceEquals(factory, _autoCryptClientControllerFactory))
            {
                return;
            }

            if (_autoCryptClientControllerFactory != null)
            {
                throw new MongoConfigurationException("AutoEncryption Provider already registered.");
            }
            _autoCryptClientControllerFactory = factory;
        }

        public IAutoEncryptionLibMongoCryptController CreateAutoCryptClientController(IMongoClient client, AutoEncryptionOptions autoEncryptionOptions)
        {
            if (_autoCryptClientControllerFactory == null)
            {
                throw new MongoConfigurationException("No AutoEncryption provider has been registered.");
            }
            return _autoCryptClientControllerFactory(client, autoEncryptionOptions);
        }
    }
}
