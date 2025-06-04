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
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    ///
    /// </summary>
    public static class VulnerableCryptography
    {
        // Weak hashing - Semgrep should flag
        /// <summary>
        ///
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string HashPassword(string password)
        {
            using (var md5 = MD5.Create()) // VULNERABLE - MD5 is weak
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hash);
            }
        }

        private static string Hash(string str)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            using (SHA256 algorithm = SHA256.Create())
            {
                var hash = algorithm.ComputeHash(bytes);

                return BsonUtils.ToHexString(hash);
            }
        }

        // Hardcoded encryption key - Semgrep should flag
        private static readonly byte[] EncryptionKey = {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10
        };

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] EncryptData(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = EncryptionKey; // VULNERABLE - hardcoded key
                // ... encryption logic
                return data; // simplified
            }
        }
    }
}