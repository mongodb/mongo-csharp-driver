/* Copyright 2015-present MongoDB Inc.
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
using System.Security.Cryptography;
using System.Text;
#if NET452
using MongoDB.Driver.Core.Authentication.Vendored;
#endif
using MongoDB.Driver.Core.Misc;

// use our vendored version of Rfc2898DeriveBytes because .NET Standard 1.5 and .NET Framework 4.5 do not support
// a version of Rfc2898DeriveBytes that allows us to specify to hash algorithm to be used
using Rfc2898DeriveBytes = MongoDB.Driver.Core.Authentication.Vendored.Rfc2898DeriveBytes;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// A SCRAM-SHA1 SASL authenticator.
    /// </summary>
    public sealed class ScramSha1Authenticator : ScramShaAuthenticator
    {
        // static properties
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        /// <value>
        /// The name of the mechanism.
        /// </value>
        public static string MechanismName => "SCRAM-SHA-1";

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ScramSha1Authenticator"/> class.
        /// </summary>
        /// <param name="credential">The credential.</param>
        [Obsolete("Use the newest overload instead.")]
        public ScramSha1Authenticator(UsernamePasswordCredential credential)
            : this(credential, serverApi: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScramSha1Authenticator"/> class.
        /// </summary>
        /// <param name="credential">The credential.</param>
        /// <param name="serverApi">The server API.</param>
        public ScramSha1Authenticator(UsernamePasswordCredential credential, ServerApi serverApi)
            : this(credential, new DefaultRandomStringGenerator(), serverApi)
        {
        }

        internal ScramSha1Authenticator(
            UsernamePasswordCredential credential,
            IRandomStringGenerator randomStringGenerator,
            ServerApi serverApi)
            : base(credential, HashAlgorithmName.SHA1, randomStringGenerator, H1, Hi1, Hmac1, new ScramCache(), serverApi)
        {
        }

        private static byte[] H1(byte[] data)
        {
            using (var sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(data);
            }
        }

        private static byte[] Hi1(UsernamePasswordCredential credential, byte[] salt, int iterations)
        {
            var passwordDigest = AuthenticationHelper.MongoPasswordDigest(credential.Username, credential.Password);
            // 20 is the length of output of a sha-1 hmac
            return new Rfc2898DeriveBytes(passwordDigest, salt, iterations).GetBytes(20);
        }

        private static byte[] Hmac1(UTF8Encoding encoding, byte[] data, string key)
        {
#if NET452
            using (var hmac = new HMACSHA1(data, useManagedSha1: true))
#else
            using (var hmac = new HMACSHA1(data))
#endif
            {
                return hmac.ComputeHash(encoding.GetBytes(key));
            }
        }
    }
}
