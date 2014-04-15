/* Copyright 2010-2014 MongoDB Inc.
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
using System.Security;

namespace MongoDB.Driver.Communication.Security.Mechanisms.Sspi
{
    /// <summary>
    /// A SecBufferDesc structure.
    /// </summary>
    /// <remarks>
    /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa379815(v=vs.85).aspx
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityBufferDescriptor : IDisposable
    {
        // public fields
        public SecurityBufferType BufferType;
        public int NumBuffers;
        public IntPtr BufferPtr; //Point to SecBuffer

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityBufferDescriptor" /> struct.
        /// </summary>
        /// <param name="bufferSize">Size of the buffer.</param>
        public SecurityBufferDescriptor(int bufferSize)
        {
            BufferType = SecurityBufferType.Version;
            NumBuffers = 1;
            var buffer = new SecurityBuffer(bufferSize);
            BufferPtr = Marshal.AllocHGlobal(Marshal.SizeOf(buffer));
            Marshal.StructureToPtr(buffer, BufferPtr, false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityBufferDescriptor" /> struct.
        /// </summary>
        /// <param name="secBufferBytes">The sec buffer bytes.</param>
        public SecurityBufferDescriptor(byte[] secBufferBytes)
        {
            BufferType = SecurityBufferType.Version;
            NumBuffers = 1;
            var buffer = new SecurityBuffer(secBufferBytes);
            BufferPtr = Marshal.AllocHGlobal(Marshal.SizeOf(buffer));
            Marshal.StructureToPtr(buffer, BufferPtr, false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityBufferDescriptor" /> struct.
        /// </summary>
        /// <param name="buffers">The buffers.</param>
        /// <exception cref="System.ArgumentException">cannot be null or 0 length;buffers</exception>
        public SecurityBufferDescriptor(SecurityBuffer[] buffers)
        {
            if (buffers == null || buffers.Length == 0)
            {
                throw new ArgumentException("cannot be null or 0 length", "buffers");
            }

            BufferType = SecurityBufferType.Version;
            NumBuffers = buffers.Length;

            //Allocate memory for SecBuffer Array....
            BufferPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SecurityBuffer)) * NumBuffers);

            for (int i = 0; i < buffers.Length; i++)
            {
                var currentBuffer = buffers[i];
                var currentOffset = i * Marshal.SizeOf(typeof(SecurityBuffer));
                Marshal.WriteInt32(BufferPtr, currentOffset, currentBuffer.Count);

                var length = currentOffset + Marshal.SizeOf(typeof(int));
                Marshal.WriteInt32(BufferPtr, length, (int)currentBuffer.BufferType);

                length = currentOffset + Marshal.SizeOf(typeof(int)) + Marshal.SizeOf(typeof(int));
                Marshal.WriteIntPtr(BufferPtr, length, currentBuffer.Token);
            }
        }

        // public methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (BufferPtr != IntPtr.Zero)
            {
                if (NumBuffers == 1)
                {
                    var buffer = (SecurityBuffer)Marshal.PtrToStructure(BufferPtr, typeof(SecurityBuffer));
                    buffer.Dispose();
                }
                else
                {
                    // Since we aren't sending any messages using the kerberos encrypt/decrypt.
                    // The 1st buffer is going to be empty. We can skip it.
                    for (int i = 1; i < NumBuffers; i++)
                    {
                        var currentOffset = i * Marshal.SizeOf(typeof(SecurityBuffer));
                        var totalLength = currentOffset + Marshal.SizeOf(typeof(int)) + Marshal.SizeOf(typeof(int));
                        var buffer = Marshal.ReadIntPtr(BufferPtr, totalLength);
                        Marshal.FreeHGlobal(buffer);
                    }
                }

                Marshal.FreeHGlobal(BufferPtr);
                BufferPtr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// To the byte array.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Object has already been disposed!!!</exception>
        public byte[] ToByteArray()
        {
            byte[] bytes = null;

            if (BufferPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Object has already been disposed!!!");
            }

            if (NumBuffers == 1)
            {
                var buffer = (SecurityBuffer)Marshal.PtrToStructure(BufferPtr, typeof(SecurityBuffer));

                if (buffer.Count > 0)
                {
                    bytes = new byte[buffer.Count];
                    Marshal.Copy(buffer.Token, bytes, 0, buffer.Count);
                }
            }
            else
            {
                var bytesToAllocate = 0;

                for (int i = 0; i < NumBuffers; i++)
                {
                    var currentOffset = i * Marshal.SizeOf(typeof(SecurityBuffer));
                    bytesToAllocate += Marshal.ReadInt32(BufferPtr, currentOffset);
                }

                bytes = new byte[bytesToAllocate];

                for (int i = 0, bufferIndex = 0; i < NumBuffers; i++)
                {
                    var currentOffset = i * Marshal.SizeOf(typeof(SecurityBuffer));
                    var bytesToCopy = Marshal.ReadInt32(BufferPtr, currentOffset);
                    var length = currentOffset + Marshal.SizeOf(typeof(int)) + Marshal.SizeOf(typeof(int));
                    IntPtr SecBufferpvBuffer = Marshal.ReadIntPtr(BufferPtr, length);
                    Marshal.Copy(SecBufferpvBuffer, bytes, bufferIndex, bytesToCopy);
                    bufferIndex += bytesToCopy;
                }
            }

            return (bytes);
        }
    }
}