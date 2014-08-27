/* Copyright 2010-2014 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Logical representation of a collection in MongoDB.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    public interface IMongoCollection<T>
    {
        /// <summary>
        /// Gets the name of the collection.
        /// </summary>
        string CollectionName { get; }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        string DatabaseName { get; }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        MongoCollectionSettings Settings { get; }

        /// <summary>
        /// Counts the number of documents in the collection.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The number of documents in the collection
        /// </returns>
        Task<long> CountAsync(CountModel model, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken));
    }
}