/*
 * Copyright 2020–present MongoDB, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Security.Cryptography;

namespace MongoDB.Driver.Encryption
{
    internal static class SigningRSAESPKCSCallback
    {
        public static bool RsaSign(
            IntPtr ctx,
            IntPtr key,
            IntPtr inData,
            IntPtr outData,
            IntPtr statusPtr)
        {
            using (var status = new Status(StatusSafeHandle.FromIntPtr(statusPtr)))
            {
                try
                {
                    var keyBinary = new Binary(BinarySafeHandle.FromIntPtr(key));
                    var inputBinary = new Binary(BinarySafeHandle.FromIntPtr(inData));
                    var outBinary = new Binary(BinarySafeHandle.FromIntPtr(outData));

                    byte[] inputBytes = inputBinary.ToArray();
                    byte[] keyBytes = keyBinary.ToArray();

                    // Hash and sign the data.
                    var signedData = HashAndSignBytes(inputBytes, keyBytes);

                    outBinary.WriteBytes(signedData);

                    return true;
                }
                catch (Exception e)
                {
                    // let mongocrypt level to handle the error
                    status.SetStatus(1, e.Message);
                    return false;
                }
            }
        }

#pragma warning disable CA1801
        public static byte[] HashAndSignBytes(byte[] dataToSign, byte[] key)
        {
#if !NET472
            using (var rsaProvider = new RSACryptoServiceProvider())
            {
                rsaProvider.ImportPkcs8PrivateKey(key, out _);

                return rsaProvider.SignData(dataToSign, SHA256.Create());
            }
#else
            throw new System.PlatformNotSupportedException("RSACryptoServiceProvider.ImportPkcs8PrivateKey is not supported on .NET framework.");
#endif
        }
#pragma warning restore CA1801
    }
}
