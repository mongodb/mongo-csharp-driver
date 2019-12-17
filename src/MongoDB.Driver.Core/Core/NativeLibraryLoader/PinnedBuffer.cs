/* Copyright 2019–present MongoDB Inc.
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

namespace MongoDB.Driver.Core.NativeLibraryLoader
{
    internal class PinnedBuffer : IDisposable
    {
        private GCHandle _handle;
        private readonly IntPtr _intPtr;

        public PinnedBuffer(byte[] bytes, int offset)
        {
            // The array must be pinned by using a GCHandle before it is passed to UnsafeAddrOfPinnedArrayElement.
            // For maximum performance, this method does not validate the array passed to it; this can result in unexpected behavior.
            _handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            _intPtr = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, offset);
        }

        public IntPtr IntPtr => _intPtr;

        // public methods
        public void Dispose()
        {
            try
            {
                _handle.Free();
            }
            catch
            {
                // ignore exceptions
            }
        }
    }
}
