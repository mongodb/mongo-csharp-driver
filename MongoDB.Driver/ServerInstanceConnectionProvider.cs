using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Operations;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a provider of connections to a specific server instance.
    /// </summary>
    public class ServerInstanceConnectionProvider : IConnectionProvider
    {
        // private fields
        private readonly MongoServerInstance _serverInstance;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerInstanceConnectionProvider"/> class.
        /// </summary>
        /// <param name="serverInstance">The server instance.</param>
        /// <exception cref="System.ArgumentNullException">serverInstance</exception>
        public ServerInstanceConnectionProvider(MongoServerInstance serverInstance)
        {
            if (serverInstance == null)
            {
                throw new ArgumentNullException("serverInstance");
            }
            _serverInstance = serverInstance;
        }

        // public methods
        /// <summary>
        /// Acquires a connection.
        /// </summary>
        /// <returns>
        /// A connection.
        /// </returns>
        public MongoConnection AcquireConnection()
        {
            return _serverInstance.AcquireConnection();
        }

        /// <summary>
        /// Releases a connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public void ReleaseConnection(MongoConnection connection)
        {
            _serverInstance.ReleaseConnection(connection);
        }
    }
}
