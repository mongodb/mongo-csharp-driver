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
    /// See the TargetDataRep parameter at 
    /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa375507(v=vs.85).aspx
    /// </remarks>
    internal enum DataRepresentation
    {
        /// <summary>
        /// SECURITY_NETWORK_DREP
        /// </summary>
        Network = 0,
        /// <summary>
        /// SECURITY_NATIVE_DREP
        /// </summary>
        Native = 16
    }
}