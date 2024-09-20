/* Copyright 2021-present MongoDB Inc.
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

namespace MongoDB.Driver.Authentication
{
    /// <summary>
    /// Thrown from a GSSAPI-related method.
    /// </summary>
    public class GssapiException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GssapiException" /> class.
        /// </summary>
        protected GssapiException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GssapiException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        protected GssapiException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GssapiException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected GssapiException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
