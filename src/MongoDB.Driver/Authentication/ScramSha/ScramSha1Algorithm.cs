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

using System.Security.Cryptography;
using System.Text;

// Use our vendored version of Rfc2898DeriveBytes for .NET Standard 2.0
// because this target does not support a version of Rfc2898DeriveBytes that allows to specify the hash algorithm
#if NETSTANDARD2_0
using Rfc2898DeriveBytes = MongoDB.Driver.Authentication.Vendored.Rfc2898DeriveBytes;
#else
using Rfc2898DeriveBytes = System.Security.Cryptography.Rfc2898DeriveBytes;
#endif

namespace MongoDB.Driver.Authentication.ScramSha
{
    internal sealed class ScramSha1Algorithm : IScramShaAlgorithm
    {
        public byte[] H(byte[] data)
        {
            using (var sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(data);
            }
        }

        public byte[] Hi(UsernamePasswordCredential credential, byte[] salt, int iterations)
        {
            var passwordDigest = AuthenticationHelper.MongoPasswordDigest(credential.Username, credential.Password);

            using (var deriveBytes = new Rfc2898DeriveBytes(passwordDigest, salt, iterations, HashAlgorithmName.SHA1))
            {
                // 20 is the length of output of a sha-1 hmac
                return deriveBytes.GetBytes(20);
            }
        }

        public byte[] Hmac(UTF8Encoding encoding, byte[] data, string key)
        {
#if NET6_0_OR_GREATER
            using (var hmac = new HMACSHA1(data))
#else
            using (var hmac = new HMACSHA1(data, useManagedSha1: true))
#endif
            {
                return hmac.ComputeHash(encoding.GetBytes(key));
            }
        }
    }
}
