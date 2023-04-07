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
using System.Runtime.Serialization;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB authentication exception.
    /// </summary>
    [Serializable]
    public class MongoAuthenticationException : MongoConnectionException
    {
        private readonly bool _allowReauthentication;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoAuthenticationException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="message">The error message.</param>
        public MongoAuthenticationException(ConnectionId connectionId, string message)
            : base(connectionId, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoAuthenticationException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="allowReauthentication">Determines whether reauthentication is allowed.</param>
        public MongoAuthenticationException(ConnectionId connectionId, string message, Exception innerException, bool allowReauthentication = false)
            : base(connectionId, message, innerException)
        {
            _allowReauthentication = allowReauthentication;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoAuthenticationException"/> class.
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        public MongoAuthenticationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _allowReauthentication = (bool)info.GetValue(nameof(_allowReauthentication), typeof(bool));
        }

        /// <inheritdoc/>
        public override bool IsNetworkException => false;

        /// <summary>
        /// Allows reauthentication flag.
        /// </summary>
        public virtual bool AllowReauthentication => _allowReauthentication;

        // methods
        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(_allowReauthentication), _allowReauthentication);
        }
    }
}
