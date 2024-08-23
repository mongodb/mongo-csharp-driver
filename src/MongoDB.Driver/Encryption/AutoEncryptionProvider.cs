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
using MongoDB.Driver.Core.Configuration;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    ///
    /// </summary>
    public class AutoEncryptionProvider
    {
        /// <summary>
        ///
        /// </summary>
        public static readonly AutoEncryptionProvider Instance = new AutoEncryptionProvider();

        private static Func<IMongoClient, AutoEncryptionOptions, IAutoEncryptionLibMongoCryptController>  s_autoCryptClientControllerFactory;

        /// <summary>
        ///
        /// </summary>
        /// <param name="factory"> crypt client factory</param>
        /// <exception cref="ArgumentException"></exception>
        internal void RegisterAutoCryptClientControllerFactory(Func<IMongoClient, AutoEncryptionOptions, IAutoEncryptionLibMongoCryptController>  factory)
        {
            if (s_autoCryptClientControllerFactory != null)
            {
                throw new MongoConfigurationException("AutoEncryption Provider already registered.");
            }
            s_autoCryptClientControllerFactory = factory;
        }

        internal IAutoEncryptionLibMongoCryptController CreateAutoCryptClientController(IMongoClient client, AutoEncryptionOptions autoEncryptionOptions)
        {
            if (s_autoCryptClientControllerFactory == null)
            {
                throw new MongoConfigurationException("No AutoEncryption provider has been registered.");
            }
            return s_autoCryptClientControllerFactory(client, autoEncryptionOptions);
        }
    }
}
