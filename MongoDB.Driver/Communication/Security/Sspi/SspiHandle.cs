using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace MongoDB.Driver.Security.Sspi
{
    /// <summary>
    /// A SecHandle structure.
    /// </summary>
    /// <remarks>
    /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa380495(v=vs.85).aspx
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SspiHandle
    {
        // private fields
        private IntPtr _hi;
        private IntPtr _low;

        // public properties
        /// <summary>
        /// Gets a value indicating whether this instance is zero.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is zero; otherwise, <c>false</c>.
        /// </value>
        public bool IsZero
        {
            get
            {
                if (_hi != IntPtr.Zero)
                {
                    return false;
                }
                else
                {
                    return _low == IntPtr.Zero;
                }
            }
        }

        // public methods
        /// <summary>
        /// Sets to invalid.
        /// </summary>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public void SetToInvalid()
        {
            _hi = IntPtr.Zero;
            _low = IntPtr.Zero;
        }
    }
}