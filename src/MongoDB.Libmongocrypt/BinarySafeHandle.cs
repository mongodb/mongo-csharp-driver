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
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace MongoDB.Libmongocrypt
{
    /// <summary>
    /// SafeHandle to manage the lifetime of a mongocrypt_binary_t.
    /// </summary>
    /// <seealso cref="System.Runtime.InteropServices.SafeHandle" />
    internal class BinarySafeHandle : SafeHandle
    {
        private BinarySafeHandle()
            : base(IntPtr.Zero, true)
        {
        }

        private BinarySafeHandle(IntPtr ptr)
            : base(ptr, false)
        {
        }

        public static BinarySafeHandle FromIntPtr(IntPtr ptr)
        {
            return new BinarySafeHandle(ptr);
        }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            // Here, we must obey all rules for constrained execution regions.
            Library.mongocrypt_binary_destroy(handle);
            return true;
        }
    }
}
