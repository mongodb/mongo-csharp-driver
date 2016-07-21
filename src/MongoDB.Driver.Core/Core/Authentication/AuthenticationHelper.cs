/* Copyright 2013-2016 MongoDB Inc.
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
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication
{
    internal static class AuthenticationHelper
    {
        public static void Authenticate(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));

            // authentication is currently broken on arbiters
            if (!description.IsMasterResult.IsArbiter)
            {
                foreach (var authenticator in connection.Settings.Authenticators)
                {
                    authenticator.Authenticate(connection, description, cancellationToken);
                }
            }
        }

        public static async Task AuthenticateAsync(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));

            // authentication is currently broken on arbiters
            if (!description.IsMasterResult.IsArbiter)
            {
                foreach (var authenticator in connection.Settings.Authenticators)
                {
                    await authenticator.AuthenticateAsync(connection, description, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public static string MongoPasswordDigest(string username, SecureString password)
        {
            using (var md5 = MD5.Create())
            {
                var prefix = Utf8Encodings.Strict.GetBytes(username + ":mongo:");
                
                IntPtr unmanagedPassword = IntPtr.Zero;
                var passwordChars = new char[password.Length];
                try
                {
#if NET45
                    unmanagedPassword = Marshal.SecureStringToGlobalAllocUnicode(password);
#else
                    unmanagedPassword = SecureStringMarshal.SecureStringToGlobalAllocUnicode(password);
#endif
                    Marshal.Copy(unmanagedPassword, passwordChars, 0, passwordChars.Length);
                    
                    var passwordBytesCount = Utf8Encodings.Strict.GetByteCount(passwordChars);
                    var buffer = new byte[prefix.Length + passwordBytesCount];
                    try
                    {
                        Buffer.BlockCopy(prefix, 0, buffer, 0, prefix.Length);
                        Utf8Encodings.Strict.GetBytes(passwordChars, 0, passwordChars.Length, buffer, prefix.Length);

                        return BsonUtils.ToHexString(md5.ComputeHash(buffer));
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
}
