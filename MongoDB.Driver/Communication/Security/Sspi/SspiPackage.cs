using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Driver.Security.Sspi
{
    /// <summary>
    /// This is represented as a string in AcquireCredentialsHandle. This value will have .ToString() called on it.
    /// </summary>
    internal enum SspiPackage
    {
        /// <summary>
        /// Kerberos
        /// </summary>
        Kerberos
    }
}
