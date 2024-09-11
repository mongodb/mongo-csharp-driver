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
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson.IO;

namespace MongoDB.Driver.Authentication.ScramSha
{
    internal sealed class ScramSha256Algorithm : IScramShaAlgorithm
    {
        public byte[] H(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(data);
            }
        }

        public byte[] Hi(UsernamePasswordCredential credential, byte[] salt, int iterations)
        {
            var passwordIntPtr = Marshal.SecureStringToGlobalAllocUnicode(credential.SaslPreppedPassword);
            try
            {
                var passwordChars = new char[credential.SaslPreppedPassword.Length];
                var passwordCharsHandle = GCHandle.Alloc(passwordChars, GCHandleType.Pinned);
                try
                {
                    Marshal.Copy(passwordIntPtr, passwordChars, 0, credential.SaslPreppedPassword.Length);
                    return Hi256(passwordChars, salt, iterations);
                }
                finally
                {
                    Array.Clear(passwordChars, 0, passwordChars.Length);
                    passwordCharsHandle.Free();
                }
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(passwordIntPtr);
            }
        }

        private static byte[] Hi256(char[] passwordChars, byte[] salt, int iterations)
        {
            var passwordBytes = new byte[Utf8Encodings.Strict.GetByteCount(passwordChars)];
            var passwordBytesHandle = GCHandle.Alloc(passwordBytes, GCHandleType.Pinned);

            try
            {
                Utf8Encodings.Strict.GetBytes(passwordChars, 0, passwordChars.Length, passwordBytes, 0);

                using (var deriveBytes = new Rfc2898DeriveBytes(passwordBytes, salt, iterations, HashAlgorithmName.SHA256))
                {
                    // 32 is the length of output of a sha-256 hmac
                    return deriveBytes.GetBytes(32);
                }
            }
            finally
            {
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
                passwordBytesHandle.Free();
            }
        }

        public byte[] Hmac(UTF8Encoding encoding, byte[] data, string key)
        {
            using (var hmac = new HMACSHA256(data))
            {
                return hmac.ComputeHash(encoding.GetBytes(key));
            }
        }
    }
}
