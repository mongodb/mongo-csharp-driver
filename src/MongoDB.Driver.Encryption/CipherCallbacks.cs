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
    internal enum CryptMode
    {
        Encrypt,
        Decrypt
    }

    internal static class CipherCallbacks
    {
        public static bool EncryptCbc(
            IntPtr ctx,
            IntPtr key,
            IntPtr iv,
            IntPtr @in,
            IntPtr @out,
            ref uint bytesWritten,
            IntPtr statusPtr)
        {
            using (var status = new Status(StatusSafeHandle.FromIntPtr(statusPtr)))
            {
                try
                {
                    var keyBinary = new Binary(BinarySafeHandle.FromIntPtr(key));
                    var inputBinary = new Binary(BinarySafeHandle.FromIntPtr(@in));
                    var outputBinary = new Binary(BinarySafeHandle.FromIntPtr(@out));
                    var ivBinary = new Binary(BinarySafeHandle.FromIntPtr(iv));

                    byte[] keyBytes = keyBinary.ToArray();
                    byte[] ivBytes = ivBinary.ToArray();
                    byte[] inputBytes = inputBinary.ToArray();

                    var outputBytes = AesCrypt(keyBytes, ivBytes, inputBytes, CryptMode.Encrypt, CipherMode.CBC);
                    bytesWritten = (uint)outputBytes.Length;
                    outputBinary.WriteBytes(outputBytes);
                    return true;
                }
                catch (Exception e)
                {
                    status.SetStatus(1, e.Message);
                    return false;
                }
            }
        }

        public static bool DecryptCbc(
            IntPtr ctx,
            IntPtr key,
            IntPtr iv,
            IntPtr @in,
            IntPtr @out,
            ref uint bytesWritten,
            IntPtr statusPtr)
        {
            using (var status = new Status(StatusSafeHandle.FromIntPtr(statusPtr)))
            {
                try
                {
                    var keyBinary = new Binary(BinarySafeHandle.FromIntPtr(key));
                    var inputBinary = new Binary(BinarySafeHandle.FromIntPtr(@in));
                    var outputBinary = new Binary(BinarySafeHandle.FromIntPtr(@out));
                    var ivBinary = new Binary(BinarySafeHandle.FromIntPtr(iv));

                    byte[] keyBytes = keyBinary.ToArray();
                    byte[] ivBytes = ivBinary.ToArray();
                    byte[] inputBytes = inputBinary.ToArray();

                    var outputBytes = AesCrypt(keyBytes, ivBytes, inputBytes, CryptMode.Decrypt, CipherMode.CBC);
                    bytesWritten = (uint)outputBytes.Length;
                    outputBinary.WriteBytes(outputBytes);

                    return true;
                }
                catch (Exception e)
                {
                    status.SetStatus(1, e.Message);
                    return false;
                }
            }
        }

        public static bool EncryptEcb(
            IntPtr ctx,
            IntPtr key,
            IntPtr iv,
            IntPtr @in,
            IntPtr @out,
            ref uint bytesWritten,
            IntPtr statusPtr)
        {
            using (var status = new Status(StatusSafeHandle.FromIntPtr(statusPtr)))
            {
                try
                {
                    var keyBinary = new Binary(BinarySafeHandle.FromIntPtr(key));
                    var inputBinary = new Binary(BinarySafeHandle.FromIntPtr(@in));
                    var outputBinary = new Binary(BinarySafeHandle.FromIntPtr(@out));
                    var ivBinary = new Binary(BinarySafeHandle.FromIntPtr(iv));

                    byte[] keyBytes = keyBinary.ToArray();
                    byte[] ivBytes = ivBinary.ToArray();
                    byte[] inputBytes = inputBinary.ToArray();

                    var outputBytes = AesCrypt(keyBytes, ivBytes, inputBytes, CryptMode.Encrypt, CipherMode.ECB);
                    bytesWritten = (uint)outputBytes.Length;
                    outputBinary.WriteBytes(outputBytes);
                    return true;
                }
                catch (Exception e)
                {
                    status.SetStatus(1, e.Message);
                    return false;
                }
            }
        }

        public static byte[] AesCrypt(byte[] keyBytes, byte[] ivBytes, byte[] inputBytes, CryptMode cryptMode, CipherMode cipherMode)
        {
            using (var aes = new RijndaelManaged())
            {
                aes.Mode = cipherMode;

                aes.Key = keyBytes;
                if (ivBytes.Length > 0)
                {
                    aes.IV = ivBytes;
                }

                aes.Padding = PaddingMode.None; // mongocrypt level is responsible for padding

                using (var encrypto = CreateCryptoTransform(aes))
                {
                    byte[] encryptedBytes = encrypto.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                    return encryptedBytes;
                }

                ICryptoTransform CreateCryptoTransform(RijndaelManaged rijndaelManaged)
                {
                    switch (cryptMode)
                    {
                        case CryptMode.Encrypt: return rijndaelManaged.CreateEncryptor();
                        case CryptMode.Decrypt: return rijndaelManaged.CreateDecryptor();
                        default: throw new InvalidOperationException($"Unsupported crypt mode {cryptMode}."); // should not be reached
                    }
                }
            }
        }
    }
}
