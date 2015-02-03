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

using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    /// <preliminary/>
    /// <summary>
    /// Represents information about a ServerAfterDescriptionChanged event.
    /// </summary>
    public struct ServerAfterDescriptionChangedEvent
    {
        private readonly ServerDescription _oldDescription;
        private readonly ServerDescription _newDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerAfterDescriptionChangedEvent"/> struct.
        /// </summary>
        /// <param name="oldDescription">The old description.</param>
        /// <param name="newDescription">The new description.</param>
        public ServerAfterDescriptionChangedEvent(ServerDescription oldDescription, ServerDescription newDescription)
        {
            _oldDescription = oldDescription;
            _newDescription = newDescription;
        }

        /// <summary>
        /// Gets the new description.
        /// </summary>
        /// <value>
        /// The new description.
        /// </value>
        public ServerDescription NewDescription
        {
            get { return _newDescription; }
        }

        /// <summary>
        /// Gets the old description.
        /// </summary>
        /// <value>
        /// The old description.
        /// </value>
        public ServerDescription OldDescription
        {
            get { return _oldDescription; }
        }
    }
}
