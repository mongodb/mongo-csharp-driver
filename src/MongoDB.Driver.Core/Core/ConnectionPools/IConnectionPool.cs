/* Copyright 2013-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.ConnectionPools
{
    /// <summary>
    /// Represents a connection pool.
    /// </summary>
    public interface IConnectionPool : IDisposable
    {
        // properties
        /// <summary>
        /// Gets the generation of the connection pool.
        /// </summary>
        /// <value>
        /// The generation.
        /// </value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1721:Property names should not match get methods", Justification = "Backward compatibility")]
        int Generation { get; }

        /// <summary>
        /// Gets the server identifier.
        /// </summary>
        /// <value>
        /// The server identifier.
        /// </value>
        ServerId ServerId { get; }

        // methods
        /// <summary>
        /// Acquires a connection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A connection.</returns>
        IConnectionHandle AcquireConnection(CancellationToken cancellationToken);

        /// <summary>
        /// Acquires a connection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a connection.</returns>
        Task<IConnectionHandle> AcquireConnectionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Clears the connection pool.
        /// </summary>
        void Clear();

        /// <summary>
        /// Clears the connection pool.
        /// </summary>
        void Clear(ObjectId serviceId);

        /// <summary>
        /// Gets the current generation for the connection pool (or service).
        /// </summary>
        /// <param name="serviceId">The optional service Id.</param>
        /// <returns>The connection pool generation.</returns>
        int GetGeneration(ObjectId? serviceId);

        /// <summary>
        /// Initializes the connection pool.
        /// </summary>
        void Initialize();
    }
}
