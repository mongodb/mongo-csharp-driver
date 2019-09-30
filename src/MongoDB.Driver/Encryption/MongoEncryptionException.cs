/* Copyright 2019-present MongoDB Inc.
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

#if NET452
using System.Runtime.Serialization;
#endif

using System;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// [Beta] Represents an encryption exception.
    /// </summary>
#if NET452
    [Serializable]
#endif
    public class MongoEncryptionException : MongoClientException
    {
        /// <summary>
        /// [Beta] Initializes a new instance of the <see cref="MongoEncryptionException"/> class.
        /// </summary>
        /// <param name="innerException">The inner exception.</param>
        public MongoEncryptionException(Exception innerException)
            : base($"Encryption related exception: {innerException.Message}.", innerException)
        {
        }
    }
}
