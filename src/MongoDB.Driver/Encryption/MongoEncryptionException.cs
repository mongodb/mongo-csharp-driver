﻿/* Copyright 2019-present MongoDB Inc.
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
using System.Runtime.Serialization;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// Represents an encryption exception.
    /// </summary>
    [Serializable]
    public class MongoEncryptionException : MongoClientException
    {
        #region static
        private static string FormatErrorMessage(string errorMessage)
        {
            errorMessage = $"Encryption related exception: {errorMessage}";
            return errorMessage.EndsWith(".") ? errorMessage : $"{errorMessage}.";
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoEncryptionException"/> class.
        /// </summary>
        /// <param name="innerException">The inner exception.</param>
        public MongoEncryptionException(Exception innerException)
            : base(FormatErrorMessage(innerException.Message), innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoEncryptionException"/> class (this overload used by deserialization).
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        protected MongoEncryptionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
