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
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Events
{
    /// <preliminary/>
    /// <summary>
    /// Represents information about a ConnectionBeforeOpening event.
    /// </summary>
    public struct ConnectionBeforeOpeningEvent
    {
        private readonly ConnectionId _connectionId;
        private readonly ConnectionSettings _connectionSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionBeforeOpeningEvent"/> struct.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="connectionSettings">The connection settings.</param>
        public ConnectionBeforeOpeningEvent(ConnectionId connectionId, ConnectionSettings connectionSettings)
        {
            _connectionId = connectionId;
            _connectionSettings = connectionSettings;
        }

        /// <summary>
        /// Gets the connection identifier.
        /// </summary>
        /// <value>
        /// The connection identifier.
        /// </value>
        public ConnectionId ConnectionId
        {
            get { return _connectionId; }
        }

        /// <summary>
        /// Gets the connection settings.
        /// </summary>
        /// <value>
        /// The connection settings.
        /// </value>
        public ConnectionSettings ConnectionSettings
        {
            get { return _connectionSettings; }
        }
    }
}
