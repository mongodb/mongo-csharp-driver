/* Copyright 2010-2013 10gen Inc.
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
using System.Collections.ObjectModel;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Abstraction for a compositing server instances based on type.
    /// </summary>
    internal interface IMongoServerProxy
    {
        /// <summary>
        /// Gets the build info.
        /// </summary>
        MongoServerBuildInfo BuildInfo { get; }

        /// <summary>
        /// Gets the connection attempt.
        /// </summary>
        int ConnectionAttempt { get; }

        /// <summary>
        /// Gets the instances.
        /// </summary>
        ReadOnlyCollection<MongoServerInstance> Instances { get; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        MongoServerState State { get; }

        /// <summary>
        /// Chooses the server instance.
        /// </summary>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>A MongoServerInstance.</returns>
        MongoServerInstance ChooseServerInstance(ReadPreference readPreference);

        /// <summary>
        /// Connects to the instances respecting the timeout and readPreference.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="readPreference">The read preference.</param>
        void Connect(TimeSpan timeout, ReadPreference readPreference);

        /// <summary>
        /// Disconnects the server.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Checks whether the server is alive (throws an exception if not).
        /// </summary>
        void Ping();

        /// <summary>
        /// Verifies the state of the server.
        /// </summary>
        void VerifyState();
    }
}