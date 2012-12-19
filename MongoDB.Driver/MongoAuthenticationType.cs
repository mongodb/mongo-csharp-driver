using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Driver
{
    /// <summary>
    /// The authentication type used to communicate with MongoDB.
    /// </summary>
    public enum MongoAuthenticationType
    {
        /// <summary>
        /// Authenticate to the server using GSSAPI.
        /// </summary>
        GSSAPI
    }
}
