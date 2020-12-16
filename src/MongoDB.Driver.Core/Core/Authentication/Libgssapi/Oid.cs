/* Copyright 2021-present MongoDB Inc.
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
using System.Runtime.InteropServices;

namespace MongoDB.Driver.Core.Authentication.Libgssapi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Oid
    {
        public uint length;
        public IntPtr elements;

        public static IntPtr NoOid = IntPtr.Zero;
        public static Oid NtUserName = Create(0x2a, 0x86, 0x48, 0x86, 0xf7, 0x12, 0x01, 0x02, 0x01, 0x01);
        public static Oid MechKrb5 = Create(0x2a, 0x86, 0x48, 0x86, 0xf7, 0x12, 0x01, 0x02, 0x02);
        public static Oid NtHostBasedService = Create(0x2a, 0x86, 0x48, 0x86, 0xf7, 0x12, 0x01, 0x02, 0x01, 0x04);

        private static Oid Create(params byte[] oidBytes)
        {
            var ntUserNameHandle = GCHandle.Alloc(oidBytes, GCHandleType.Pinned);
            return new Oid {elements = ntUserNameHandle.AddrOfPinnedObject(), length = (uint) oidBytes.Length};
        }
    }
}
