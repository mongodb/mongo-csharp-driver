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
    internal static class HashCallback
    {
        public static bool Hash(
            IntPtr ctx,
            IntPtr @in,
            IntPtr @out,
            IntPtr statusPtr)
        {
            using (var status = new Status(StatusSafeHandle.FromIntPtr(statusPtr)))
            {
                try
                {
                    var inputBinary = new Binary(BinarySafeHandle.FromIntPtr(@in));
                    var outBinary = new Binary(BinarySafeHandle.FromIntPtr(@out));

                    var outBytes = CalculateHash(inputBinary.ToArray());
                    outBinary.WriteBytes(outBytes);
                    return true;
                }
                catch (Exception ex)
                {
                    status.SetStatus(1, ex.Message);
                    return false;
                }
            }
        }

        public static byte[] CalculateHash(byte[] inputBytes)
        {
            using (var sha256 = SHA256.Create())
            {
                sha256.Initialize();
                _ = sha256.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                return sha256.Hash;
            }
        }
    }
}
