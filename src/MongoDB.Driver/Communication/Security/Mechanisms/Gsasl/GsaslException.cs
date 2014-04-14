/* Copyright 2010-2014 MongoDB Inc.
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

namespace MongoDB.Driver.Communication.Security.Mechanisms.Gsasl
{
    /// <summary>
    /// Thrown from a gsasl wrapped operation.
    /// </summary>
    [Serializable]
    public class GsaslException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GsaslException" /> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        public GsaslException(int errorCode)
        {
            HResult = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GsaslException" /> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="message">The message.</param>
        public GsaslException(int errorCode, string message)
            : base(message)
        {
            HResult = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GsaslException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected GsaslException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}