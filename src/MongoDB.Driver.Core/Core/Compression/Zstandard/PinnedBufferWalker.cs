/* Copyright 2020–present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Compression.Zstandard
{
    internal class PinnedBufferWalker : IDisposable
    {
        private readonly byte[] _bytes;
        private GCHandle _handle; // not readonly to prevent a temporary copy from being created when calling Free
        private IntPtr _intPtr;
        private int _offset;

        public PinnedBufferWalker(byte[] bytes, int offset)
        {
            _bytes = Ensure.IsNotNull(bytes, nameof(bytes));
            // The array must be pinned by using a GCHandle before it is passed to UnsafeAddrOfPinnedArrayElement.
            // For maximum performance, this method does not validate the array passed to it; this can result in unexpected behavior.
            _handle = GCHandle.Alloc(_bytes, GCHandleType.Pinned);
            _offset = offset;

            RefreshIntPtr();
        }

        public IntPtr IntPtr => _intPtr;

        public int Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                RefreshIntPtr();
            }
        }

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

        // private methods
        private void RefreshIntPtr()
        {
            _intPtr = Marshal.UnsafeAddrOfPinnedArrayElement(_bytes, _offset);
        }
    }
}
