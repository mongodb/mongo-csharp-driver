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

using MongoDB.Bson;

namespace MongoDB.Driver.Core.Authentication
{
    /// <inheritdoc/>
    public class CustomizedIsMasterCommand : ICustomizedIsMasterCommand
    {
        /// Constructor
        /// <param name="authenticator">The authenticator associated with the customized IsMaster command (if any)</param>
        /// <param name="command">The customized IsMasterCommand</param>
        public CustomizedIsMasterCommand(BsonDocument command, IAuthenticator authenticator = null)
        {
            Authenticator = authenticator;
            Command = command;
        }

        /// <inheritdoc/>
        public IAuthenticator Authenticator { get; }
        /// <inheritdoc/>
        public BsonDocument Command { get; }
    }
}
