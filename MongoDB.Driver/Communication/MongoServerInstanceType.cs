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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents an instance of a MongoDB server host (in the case of a replica set a MongoServer uses multiple MongoServerInstances).
    /// </summary>
    public enum MongoServerInstanceType
    {
        /// <summary>
        /// The server instance type is unknown.  This is the default.
        /// </summary>
        Unknown,
        /// <summary>
        /// The server is a standalone instance.
        /// </summary>
        StandAlone,
        /// <summary>
        /// The server is a replica set member.
        /// </summary>
        ReplicaSetMember,
        /// <summary>
        /// The server is a shard router (mongos).
        /// </summary>
        ShardRouter
    }
}
