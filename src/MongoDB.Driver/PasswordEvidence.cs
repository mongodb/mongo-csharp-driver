/* Copyright 2010-2016 MongoDB Inc.
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace MongoDB.Driver
{
    /// <summary>
    /// Evidence of a MongoIdentity via a shared secret.
    /// </summary>
    public sealed class PasswordEvidence : MongoIdentityEvidence
    {
        // private fields
        private readonly SecureString _securePassword;
        private readonly string _digest; // used to implement Equals without referring to the SecureString

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordEvidence" /> class.
        /// </summary>
        /// <param name="password">The password.</param>
        public PasswordEvidence(SecureString password)
        {
            _securePassword = password.Copy();
            _securePassword.MakeReadOnly();
            _digest = GenerateDigest(password);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordEvidence" /> class.
        /// </summary>
        /// <param name="password">The password.</param>
        public PasswordEvidence(string password)
            : this(CreateSecureString(password))
        { }

        // public properties
        /// <summary>
        /// Gets the password.
        /// </summary>
        public SecureString SecurePassword
        {
            get { return _securePassword; }
        }

        // public methods
        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="rhs">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }

            return _digest == ((PasswordEvidence)rhs)._digest;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return _digest.GetHashCode();
        }

        // internal methods
        /// <summary>
        /// Computes the MONGODB-CR password digest.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns></returns>
        internal string ComputeMongoCRPasswordDigest(string username)
        {
            using (var md5 = MD5.Create())
            {
                var encoding = Utf8Encodings.Strict;
                var prefixBytes = encoding.GetBytes(username + ":mongo:");
                var hash = ComputeHash(md5, prefixBytes, _securePassword);
                return BsonUtils.ToHexString(hash);
            }
        }

        // private static methods
        private static SecureString CreateSecureString(string str)
        {
            if (str != null)
            {
                var secureStr = new SecureString();
                foreach (var c in str)
                {
                    secureStr.AppendChar(c);
                }
                secureStr.MakeReadOnly();
                return secureStr;
            }

            return null;
        }

        /// <summary>
        /// Computes the hash value of the secured string 
        /// </summary>
        private static string GenerateDigest(SecureString secureString)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = ComputeHash(sha256, new byte[0], secureString);
                return BsonUtils.ToHexString(hash);
            }
        }

        private static byte[] ComputeHash(HashAlgorithm algorithm, byte[] prefixBytes, SecureString secureString)
        {
            var passwordChars = new char[secureString.Length];
#if NET45
            var unmanagedPassword = Marshal.SecureStringToGlobalAllocUnicode(secureString);
#else
            var unmanagedPassword = SecureStringMarshal.SecureStringToGlobalAllocUnicode(secureString);
#endif
            try
            {
                Marshal.Copy(unmanagedPassword, passwordChars, 0, passwordChars.Length);

                var passwordBytesCount = Utf8Encodings.Strict.GetByteCount(passwordChars);
                var buffer = new byte[prefixBytes.Length + passwordBytesCount];
                try
                {
                    Buffer.BlockCopy(prefixBytes, 0, buffer, 0, prefixBytes.Length);
                    Utf8Encodings.Strict.GetBytes(passwordChars, 0, passwordChars.Length, buffer, prefixBytes.Length);

                    return algorithm.ComputeHash(buffer);
                }
                finally
                {
                    // for security reasons
                    Array.Clear(buffer, 0, buffer.Length);
                }
            }
            finally
            {
                // for security reasons
                Array.Clear(passwordChars, 0, passwordChars.Length);

                if (unmanagedPassword != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(unmanagedPassword);
                }
            }
        }

    }
}
