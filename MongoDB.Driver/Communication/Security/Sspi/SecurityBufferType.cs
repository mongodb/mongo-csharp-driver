using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Driver.Security.Sspi
{
    /// <summary>
    /// Types for the SecurityBuffer structure.
    /// </summary>
    internal enum SecurityBufferType
    {
        /// <summary>
        /// SECBUFFER_VERSION
        /// </summary>
        Version = 0,
        /// <summary>
        /// SECBUFFER_EMPTY
        /// </summary>
        Empty = 0,
        /// <summary>
        /// SECBUFFER_DATA
        /// </summary>
        Data = 1,
        /// <summary>
        /// SECBUFFER_TOKEN
        /// </summary>
        Token = 2,
        /// <summary>
        /// SECBUFFER_PADDING
        /// </summary>
        Padding = 9,
        /// <summary>
        /// SECBUFFER_STREAM
        /// </summary>
        Stream = 10
    }
}
