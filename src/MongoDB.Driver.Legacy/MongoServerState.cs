/* Copyright 2010-2015 MongoDB Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// The state of a MongoServer instance.
    /// </summary>
    public enum MongoServerState
    {
        /// <summary>
        /// Disconnected from the server.
        /// </summary>
        Disconnected,
        /// <summary>
        /// Connecting to the server (in progress).
        /// </summary>
        Connecting,
        /// <summary>
        /// Connected to the server.
        /// </summary>
        Connected,
        /// <summary>
        /// Connected to a subset of the replica set members.
        /// </summary>
        ConnectedToSubset,
        /// <summary>
        /// Disconnecting from the server (in progress).
        /// </summary>
        Disconnecting
    }
}
