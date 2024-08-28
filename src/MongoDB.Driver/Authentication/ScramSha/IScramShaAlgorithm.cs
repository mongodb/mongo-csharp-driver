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

using System.Text;

namespace MongoDB.Driver.Authentication.ScramSha
{
    internal interface IScramShaAlgorithm
    {
        /// <summary>
        /// An H function as defined in RFC5802.
        /// </summary>
        /// <param name="data">The data to hash. Also called "str" in RFC5802.</param>
        byte[] H(byte[] data);

        /// <summary>
        /// A Hi function used to compute the SaltedPassword as defined in RFC5802, except with "str" parameter replaced
        /// with a UsernamePassword credential so that the password can be optionally digested/prepped in a secure fashion
        /// before being consumed as the "str" parameter would be in RFC5802's Hi.
        /// </summary>
        /// <param name="credentials">The credential to be digested/prepped before being consumed as the "str"
        /// parameter would be in RFC5802's Hi</param>
        /// <param name="salt">The salt.</param>
        /// <param name="iterations">The iteration count.</param>
        byte[] Hi(UsernamePasswordCredential credentials, byte[] salt, int iterations);

        /// <summary>
        /// An HMAC function as defined in RFC5802, plus the encoding of the data.
        /// </summary>
        /// <param name="encoding">The encoding of the data.</param>
        /// <param name="data">The data. Also called "str" in RFC5802.</param>
        /// <param name="key">The key.</param>
        byte[] Hmac(UTF8Encoding encoding, byte[] data, string key);
    }
}

