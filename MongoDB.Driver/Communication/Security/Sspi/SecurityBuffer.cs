using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MongoDB.Driver.Security.Sspi
{
    /// <summary>
    /// A SecBuffer structure.
    /// </summary>
    /// <remarks>
    /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa379814(v=vs.85).aspx
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityBuffer : IDisposable
    {
        // public fields
        public int Count;
        public SecurityBufferType BufferType;
        public IntPtr Token;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityBuffer" /> struct.
        /// </summary>
        /// <param name="bufferSize">Size of the buffer.</param>
        public SecurityBuffer(int bufferSize)
        {
            Count = bufferSize;
            BufferType = SecurityBufferType.Token;
            Token = Marshal.AllocHGlobal(bufferSize);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityBuffer" /> struct.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public SecurityBuffer(byte[] bytes)
        {
            Count = bytes.Length;
            BufferType = SecurityBufferType.Token;
            Token = Marshal.AllocHGlobal(Count);
            Marshal.Copy(bytes, 0, Token, Count);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityBuffer" /> struct.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="bufferType">Type of the buffer.</param>
        public SecurityBuffer(byte[] bytes, SecurityBufferType bufferType)
        {
            BufferType = bufferType;

            if (bytes != null && bytes.Length != 0)
            {
                Count = bytes.Length;
                Token = Marshal.AllocHGlobal(Count);
                Marshal.Copy(bytes, 0, Token, Count);
            }
            else
            {
                Count = 0;
                Token = IntPtr.Zero;
            }
        }

        // public methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (Token != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Token);
                Token = IntPtr.Zero;
            }
        }
    }
}