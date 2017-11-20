/* Copyright 2017 MongoDB Inc.
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
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// A class that represents no server session.
    /// </summary>
    /// <seealso cref="MongoDB.Driver.IServerSession" />
    internal sealed class NoServerSession : IServerSession
    {
        #region static
        // private static fields
        private static readonly IServerSession __instance = new NoServerSession();

        // public static fields
        /// <summary>
        /// Gets the pre-created instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static IServerSession Instance => __instance;
        #endregion

        /// <inheritdoc />
        public BsonDocument Id => null;

        /// <inheritdoc />
        public DateTime? LastUsedAt => null;

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public void WasUsed()
        {
        }
    }
}
