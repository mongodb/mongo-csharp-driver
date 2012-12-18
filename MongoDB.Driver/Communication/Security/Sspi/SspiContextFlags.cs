using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Driver.Security.Sspi
{
    /// <summary>
    /// Flags for InitiateSecurityContext.
    /// </summary>
    /// <remarks>
    /// See the fContextReq parameter at 
    /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa375507(v=vs.85).aspx
    /// </remarks>
    [Flags]
    internal enum SspiContextFlags
    {
        None = 0,
        /// <summary>
        /// ISC_REQ_MUTUAL_AUTH
        /// </summary>
        MutualAuth = 0x2,
        /// <summary>
        /// ISC_REQ_CONFIDENTIALITY
        /// </summary>
        Confidentiality = 0x10,
        /// <summary>
        /// ISC_REQ_INTEGRITY
        /// </summary>
        InitIntegrity = 0x10000
    }
}