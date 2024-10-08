/*
 * Copyright 2010–present MongoDB, Inc.
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

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// SafeHandle to manage the lifetime of a mongocrypt_ctx_t.
    /// </summary>
    /// <seealso cref="System.Runtime.InteropServices.SafeHandle" />
    internal abstract class CheckableSafeHandle : SafeHandle
    {
        internal CheckableSafeHandle() : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }

        public abstract void Check(Status status, bool success);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected abstract override bool ReleaseHandle();
    }
}
