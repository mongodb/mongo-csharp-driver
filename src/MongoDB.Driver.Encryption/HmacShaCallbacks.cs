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
    internal static class HmacShaCallbacks
    {
        public static bool HmacSha512(
            IntPtr ctx,
            IntPtr key,
            IntPtr @in,
            IntPtr @out,
            IntPtr statusPtr)
        {
            return Hmac(key, @in, @out, statusPtr, bitness: 512);
        }

        public static bool HmacSha256(
           IntPtr ctx,
           IntPtr key,
           IntPtr @in,
           IntPtr @out,
           IntPtr statusPtr)
        {
            return Hmac(key, @in, @out, statusPtr, bitness: 256);
        }

        public static byte[] CalculateHash(byte[] keyBytes, byte[] inputBytes, int bitness)
        {
            using (var hmac = GetHmacByBitness(bitness, keyBytes))
            {
                hmac.Initialize();
                _ = hmac.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                return hmac.Hash;
            }
        }

        private static HMAC GetHmacByBitness(int bitness, byte[] keyBytes)
        {
            switch (bitness)
            {
                case 256: return new HMACSHA256(keyBytes);
                case 512: return new HMACSHA512(keyBytes);
                default: throw new ArgumentOutOfRangeException($"The bitness {bitness} is unsupported."); // should not be reached
            }
        }

        private static bool Hmac(
            IntPtr key,
            IntPtr @in,
            IntPtr @out,
            IntPtr statusPtr,
            int bitness)
        {
            using (var status = new Status(StatusSafeHandle.FromIntPtr(statusPtr)))
            {
                try
                {
                    var keyBinary = new Binary(BinarySafeHandle.FromIntPtr(key));
                    var inBinary = new Binary(BinarySafeHandle.FromIntPtr(@in));
                    var outBinary = new Binary(BinarySafeHandle.FromIntPtr(@out));

                    var keyBytes = keyBinary.ToArray();
                    var inBytes = inBinary.ToArray();

                    var outBytes = CalculateHash(keyBytes, inBytes, bitness: bitness);
                    outBinary.WriteBytes(outBytes);

                    return true;
                }
                catch (Exception ex)
                {
                    // let mongocrypt level to handle the error
                    status.SetStatus(1, ex.Message);
                    return false;
                }
            }
        }
    }
}
