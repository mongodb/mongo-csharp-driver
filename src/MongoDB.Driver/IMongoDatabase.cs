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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Logical representation of database in MongoDB.
    /// </summary>
    public interface IMongoDatabase
    {
        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        string DatabaseName { get; }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        MongoDatabaseSettings Settings { get; }

        /// <summary>
        /// Drops the database.
        /// </summary>
        /// <returns>A task.</returns>
        Task DropAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the collection names.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of collection names.</returns>
        Task<IReadOnlyList<string>> GetCollectionNamesAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Runs the command.
        /// </summary>
        /// <typeparam name="T">The result type of the command.</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the command.
        /// </returns>
        Task<T> RunCommandAsync<T>(BsonDocument command, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// Extensions for <see cref="IMongoDatabase"/>.
    /// </summary>
    public static class IMongoDatabaseExtensions
    {
        /// <summary>
        /// Runs the command.
        /// </summary>
        /// <typeparam name="T">The result type of the command.</typeparam>
        /// <param name="database">The database.</param>
        /// <param name="command">The command.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the command.
        /// </returns>
        public static Task<T> RunCommandAsync<T>(this IMongoDatabase database, string command, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            var doc = BsonDocument.Parse(command);

            return database.RunCommandAsync<T>(doc, timeout, cancellationToken);
        }

        /// <summary>
        /// Runs the command.
        /// </summary>
        /// <typeparam name="T">The result type of the command.</typeparam>
        /// <param name="database">The database.</param>
        /// <param name="command">The command.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the command.
        /// </returns>
        public static Task<T> RunCommandAsync<T>(this IMongoDatabase database, object command, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            var serializer = database.Settings.SerializerRegistry.GetSerializer(command.GetType());
            var doc = new BsonDocumentWrapper(command, serializer);

            return database.RunCommandAsync<T>(doc);
        }
    }
}