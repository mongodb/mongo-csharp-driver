/* Copyright 2010-2013 10gen Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MongoDB.Driver.Communication.Security.Mechanisms
{
    /// <summary>
    /// A mechanism implementing the CRAM-MD5 sasl specification.
    /// </summary>
    internal class GsaslCramMD5Implementation : GsaslImplementationBase
    {
        // private fields
        private readonly string _username;
        private readonly PasswordEvidence _password;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GsaslCramMD5Implementation" /> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public GsaslCramMD5Implementation(string username, PasswordEvidence password)
            : base("CRAM-MD5", new byte[0])
        {
            _username = username;
            _password = password;
        }

        // protected methods
        /// <summary>
        /// Gets the properties that should be used in the specified mechanism.
        /// </summary>
        /// <returns>The properties.</returns>
        protected override IEnumerable<KeyValuePair<string, string>> GetProperties()
        {
            yield return new KeyValuePair<string, string>("AUTHID", _username);
            yield return new KeyValuePair<string, string>("PASSWORD", CreatePassword());
        }

        // private methods
        private string CreatePassword()
        {
            using(var md5 = MD5.Create())
            {
                var bytes = GetMongoPassword(md5, Encoding.UTF8, _username, _password.Password);
                return ToHexString(bytes);
            }
        }
    }
}