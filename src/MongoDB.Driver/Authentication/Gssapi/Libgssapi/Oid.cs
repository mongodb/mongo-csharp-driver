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

namespace MongoDB.Driver.Authentication.Gssapi.Libgssapi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Oid
    {
        #region static
        public static readonly IntPtr GSS_C_NO_OID = IntPtr.Zero;
        public static readonly Oid GSS_C_NT_USER_NAME = Create(0x2a, 0x86, 0x48, 0x86, 0xf7, 0x12, 0x01, 0x02, 0x01, 0x01);
        public static readonly Oid GSS_C_NT_HOSTBASED_SERVICE = Create(0x2a, 0x86, 0x48, 0x86, 0xf7, 0x12, 0x01, 0x02, 0x01, 0x04);

        private static Oid Create(params byte[] oidBytes)
        {
            int numBytes = oidBytes.Length;
            var unmanagedArray = Marshal.AllocHGlobal(numBytes);
            Marshal.Copy(oidBytes, 0, unmanagedArray, numBytes);
            return new Oid { elements = unmanagedArray, length = (uint)numBytes };
        }
        #endregion

        private uint length;
        private IntPtr elements;
    }
}
