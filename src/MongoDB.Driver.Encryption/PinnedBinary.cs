/*
 * Copyright 2019–present MongoDB, Inc.
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

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// A handle to a binary that must be either in unsafe code or pinned by the GC.
    /// </summary>
    /// <seealso cref="Binary" />
    internal class PinnedBinary : Binary
    {
        #region static
        internal static void RunAsPinnedBinary<THandle>(THandle handle, byte[] bytes, Status status, Func<THandle, BinarySafeHandle, bool> handleFunc) where THandle : CheckableSafeHandle
        {
            unsafe
            {
                fixed (byte* map = bytes)
                {
                    var ptr = (IntPtr)map;
                    using (var pinned = new PinnedBinary(ptr, (uint)bytes.Length))
                    {
                        handle.Check(status, handleFunc(handle, pinned.Handle));
                    }
                }
            }
        }
        #endregion

        internal PinnedBinary(IntPtr ptr, uint len)
            : base(Library.mongocrypt_binary_new_from_data(ptr, len))
        {
        }
    }
}
