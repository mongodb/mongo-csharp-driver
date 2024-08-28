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
    internal class GssOutputBuffer : IDisposable
    {
        // These fields are only modified by unmanaged libgssapi functions.
        private readonly nuint _length = 0;
        private readonly IntPtr _value = IntPtr.Zero;

        ~GssOutputBuffer()
        {
            Dispose(false);
        }

        public IntPtr Value => _value;

        public byte[] ToByteArray()
        {
            if (_length > int.MaxValue)
            {
                throw new InvalidOperationException("GssOutputBuffer too large to read into a managed array.");
            }

            var result = new byte[_length];
            if (_length > 0)
            {
                Marshal.Copy(_value, result, 0, (int)_length);
            }
            return result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // disposing is unused because we are only cleaning up
            // unmanaged resources belonging to this class.
            if (_value != IntPtr.Zero)
            {
                _ = NativeMethods.gss_release_buffer(out _, this);
            }
        }
    }
}
