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

namespace MongoDB.Driver.Communication.Security.Mechanisms.Sspi
{
    /// <summary>
    /// Thrown from a win32 wrapped operation.
    /// </summary>
    [Serializable]
    public class Win32Exception : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Win32Exception" /> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        public Win32Exception(long errorCode)
        {
            HResult = (int)errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Win32Exception" /> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="message">The message.</param>
        public Win32Exception(long errorCode, string message) 
            : base(message) 
        {
            HResult = (int)errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Win32Exception" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected Win32Exception(SerializationInfo info, StreamingContext context)
            : base(info, context) 
        { 
        }
    }
}