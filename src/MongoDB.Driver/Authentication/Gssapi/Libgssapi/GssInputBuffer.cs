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
    internal sealed class GssInputBuffer : IDisposable
    {
        private nuint _length;
        private IntPtr _value;

        public GssInputBuffer(string inputString)
        {
            _length = (nuint)inputString.Length;
            _value = Marshal.StringToHGlobalAnsi(inputString);
        }

        public GssInputBuffer(byte[] inputBytes)
        {
            if (inputBytes == null)
            {
                _length = 0;
                _value = default;
                return;
            }

            int numBytes = inputBytes.Length;
            var unmanagedArray = Marshal.AllocHGlobal(numBytes);
            Marshal.Copy(inputBytes, 0, unmanagedArray, numBytes);

            _length = (nuint)numBytes;
            _value = unmanagedArray;
        }

        ~GssInputBuffer()
        {
            Dispose(false);
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
                Marshal.FreeHGlobal(_value);
                _length = 0;
                _value = IntPtr.Zero;
            }
        }
    }
}
