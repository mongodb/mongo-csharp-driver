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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Evidence of a MongoIdentity via a shared secret.
    /// </summary>
    public sealed class PasswordEvidence : MongoIdentityEvidence
    {
        // private fields
#if !NETCORE
        private readonly SecureString _securePassword;
        private readonly string _digest; // used to implement Equals without referring to the SecureString
#else
        private readonly string _password;
#endif

        // constructors
#if !NETCORE
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
#else
        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordEvidence" /> class.
        /// </summary>
        /// <param name="password">The password.</param>
        public PasswordEvidence(string password)
        {
            _password = password;
        }
#endif

        // public properties
#if !NETCORE
        /// <summary>
        /// Gets the password.
        /// </summary>
        public SecureString SecurePassword
        {
            get { return _securePassword; }
        }
#else
        /// <summary>
        /// Gets the password.
        /// </summary>
        public string Password
        {
            get { return _password; }
        }
#endif

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

#if !NETCORE
            return _digest == ((PasswordEvidence)rhs)._digest;
#else
            return _password == ((PasswordEvidence)rhs)._password;
#endif
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
#if !NETCORE
            return _digest.GetHashCode();
#else
            return _password.GetHashCode();
#endif
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
#if !NETCORE
                var hash = ComputeHash(md5, prefixBytes, _securePassword);
#else
                var hash = ComputeHash(md5, prefixBytes, _password);
#endif
                return BsonUtils.ToHexString(hash);
            }
        }

        // private static methods
#if !NETCORE
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
#endif

#if !NETCORE
        /// <summary>
        /// Computes the hash value of the secured string 
        /// </summary>
        private static string GenerateDigest(SecureString secureString)
        {
            using (var sha256 = new SHA256CryptoServiceProvider())
            {
                var hash = ComputeHash(sha256, new byte[0], secureString);
                return BsonUtils.ToHexString(hash);
            }
        }
#endif

#if !NETCORE
        private static byte[] ComputeHash(HashAlgorithm algorithm, byte[] prefixBytes, SecureString secureString)
        {
            var bstr = Marshal.SecureStringToBSTR(secureString);
            try
            {
                var passwordChars = new char[secureString.Length];
                var passwordCharsHandle = GCHandle.Alloc(passwordChars, GCHandleType.Pinned);
                try
                {
                    Marshal.Copy(bstr, passwordChars, 0, passwordChars.Length);

                    var passwordBytes = new byte[secureString.Length * 3]; // worst case for UTF16 to UTF8 encoding
                    var passwordBytesHandle = GCHandle.Alloc(passwordBytes, GCHandleType.Pinned);
                    try
                    {
                        var encoding = Utf8Encodings.Strict;
                        var passwordUtf8Length = encoding.GetBytes(passwordChars, 0, passwordChars.Length, passwordBytes, 0);
                        var buffer = new byte[prefixBytes.Length + passwordUtf8Length];
                        Buffer.BlockCopy(prefixBytes, 0, buffer, 0, prefixBytes.Length);
                        Buffer.BlockCopy(passwordBytes, 0, buffer, prefixBytes.Length, passwordUtf8Length);
                        var hash = algorithm.ComputeHash(buffer);
                        Array.Clear(buffer, 0, buffer.Length);
                        return hash;
                    }
                    finally
                    {
                        Array.Clear(passwordBytes, 0, passwordBytes.Length);
                        passwordBytesHandle.Free();
                    }
                }
                finally
                {
                    Array.Clear(passwordChars, 0, passwordChars.Length);
                    passwordCharsHandle.Free();
                }
            }
            finally
            {
                Marshal.ZeroFreeBSTR(bstr);
            }
#else
        private static byte[] ComputeHash(HashAlgorithm algorithm, byte[] prefixBytes, string password)
        {
            var encoding = Utf8Encodings.Strict;
            var passwordBytes = new byte[password.Length * 3]; // worst case for UTF16 to UTF8 encoding
            var passwordUtf8Length = encoding.GetBytes(password, 0, password.Length, passwordBytes, 0);
            var buffer = new byte[prefixBytes.Length + passwordUtf8Length];
            Buffer.BlockCopy(prefixBytes, 0, buffer, 0, prefixBytes.Length);
            Buffer.BlockCopy(passwordBytes, 0, buffer, prefixBytes.Length, passwordUtf8Length);
            return algorithm.ComputeHash(buffer);
        }
#endif
    }
}
