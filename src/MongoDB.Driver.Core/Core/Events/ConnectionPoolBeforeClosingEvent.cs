/* Copyright 2013-2014 MongoDB Inc.
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

using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    /// <preliminary/>
    /// <summary>
    /// Represents information about a ConnectionPoolBeforeClosing event.
    /// </summary>
    public struct ConnectionPoolBeforeClosingEvent
    {
        private readonly ServerId _serverId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPoolBeforeClosingEvent"/> struct.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        public ConnectionPoolBeforeClosingEvent(ServerId serverId)
        {
            _serverId = serverId;
        }

        /// <summary>
        /// Gets the server identifier.
        /// </summary>
        /// <value>
        /// The server identifier.
        /// </value>
        public ServerId ServerId
        {
            get { return _serverId; }
        }
    }
}
