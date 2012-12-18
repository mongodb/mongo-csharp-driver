using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace MongoDB.Driver.Security.Sspi
{
    /// <summary>
    /// Thrown from a win32 wrapped operation.
    /// </summary>
    [Serializable]
    public class Win32Exception : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Win32Exception" /> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        public Win32Exception(long errorCode)
        {
            HResult = (int)errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Win32Exception" /> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="message">The message.</param>
        public Win32Exception(long errorCode, string message) 
            : base(message) 
        {
            HResult = (int)errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Win32Exception" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected Win32Exception(SerializationInfo info, StreamingContext context)
            : base(info, context) 
        { 
        }
    }
}