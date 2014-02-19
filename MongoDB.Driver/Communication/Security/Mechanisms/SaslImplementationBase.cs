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
 * 
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;

namespace MongoDB.Driver.Communication.Security.Mechanisms
{
    /// <summary>
    /// Base implementation for a sasl step to provide some common methods.
    /// </summary>
    internal abstract class SaslImplementationBase
    {
        // protected methods
        /// <summary>
        /// Gets the mongo password.
        /// </summary>
        /// <param name="md5">The MD5.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        protected byte[] GetMongoPassword(MD5 md5, Encoding encoding, string username, string password)
        {
            var mongoPassword = username + ":mongo:" + password;
            return md5.ComputeHash(encoding.GetBytes(mongoPassword));
        }

        /// <summary>
        /// To the hex string.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns></returns>
        protected string ToHexString(byte[] bytes)
        {
            return BsonUtils.ToHexString(bytes);
        }
    }
}