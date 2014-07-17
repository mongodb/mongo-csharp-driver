/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Bson;

namespace MongoDB.Driver.Core.Authentication
{
    internal static class AuthenticationHelper
    {
        public static string MongoUsernamePasswordDigest(string username, string password)
        {
            using (var md5 = MD5.Create())
            {
                var encoding = new UTF8Encoding(false, true);
                return HexMD5(md5, username + ":mongo:" + password, encoding);
            }
        }

        public static string HexMD5(string username, string password, string nonce)
        {
            using (var md5 = MD5.Create())
            {
                var encoding = new UTF8Encoding(false, true);
                var passwordDigest = HexMD5(md5, username + ":mongo:" + password, encoding);
                return HexMD5(md5, nonce + username + passwordDigest, encoding);
            }
        }

        private static string HexMD5(MD5 md5, string value, UTF8Encoding encoding)
        {
            var bytes = encoding.GetBytes(value);
            var hash = md5.ComputeHash(bytes);
            return BsonUtils.ToHexString(hash);
        }
    }
}