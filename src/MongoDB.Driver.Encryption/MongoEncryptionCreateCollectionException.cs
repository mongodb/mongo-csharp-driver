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
using MongoDB.Bson;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// Represents an encryption exception.
    /// </summary>
#pragma warning disable CA1032
    public class MongoEncryptionCreateCollectionException : MongoEncryptionException
#pragma warning restore CA1032
    {
        private readonly BsonDocument _encryptedFields;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoEncryptionException"/> class.
        /// </summary>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="encryptedFields">The encrypted fields.</param>
        public MongoEncryptionCreateCollectionException(Exception innerException, BsonDocument encryptedFields)
            : base(innerException)
        {
            _encryptedFields = encryptedFields;
        }

        /// <summary>
        /// The encrypted fields.
        /// </summary>
        public BsonDocument EncryptedFields => _encryptedFields;
    }
}
