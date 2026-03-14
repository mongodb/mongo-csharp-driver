/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Interface for operations that create commands dynamically based on session and connection information.
    /// </summary>
    internal interface ICommandCreator
    {
        /// <summary>
        /// Creates a command to be executed.
        /// </summary>
        /// <param name="operationContext">The operation context.</param>
        /// <param name="session">The session.</param>
        /// <param name="connectionDescription">The connection description.</param>
        /// <returns>The command document.</returns>
        BsonDocument CreateCommand(
            OperationContext operationContext,
            ICoreSession session,
            ConnectionDescription connectionDescription);
    }
}
