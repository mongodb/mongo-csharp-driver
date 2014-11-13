﻿/* Copyright 2010-2014 MongoDB Inc.
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
        DatabaseNamespace DatabaseNamespace { get; }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        MongoDatabaseSettings Settings { get; }

        /// <summary>
        /// Creates the collection with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        Task CreateCollectionAsync(string name, CreateCollectionOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Drops the collection with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        Task DropCollectionAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the collection.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="name">The name.</param>
        /// <returns>An implementation of a collection.</returns>
        IMongoCollection<TDocument> GetCollection<TDocument>(string name);

        /// <summary>
        /// Gets the collection.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="name">The name.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>An implementation of a collection.</returns>
        IMongoCollection<TDocument> GetCollection<TDocument>(string name, MongoCollectionSettings settings);

        /// <summary>
        /// Gets the collection names.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The list of collection names.
        /// </returns>
        Task<IReadOnlyList<string>> GetCollectionNamesAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Renames the collection.
        /// </summary>
        /// <param name="oldName">The old name.</param>
        /// <param name="newName">The new name.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        Task RenameCollectionAsync(string oldName, string newName, RenameCollectionOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Runs the command.
        /// </summary>
        /// <typeparam name="T">The result type of the command.</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the command.
        /// </returns>
        Task<T> RunCommandAsync<T>(object command, CancellationToken cancellationToken = default(CancellationToken));
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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the command.
        /// </returns>
        public static Task<T> RunCommandAsync<T>(this IMongoDatabase database, string command, CancellationToken cancellationToken = default(CancellationToken))
        {
            return database.RunCommandAsync<T>(command, cancellationToken);
        }

        /// <summary>
        /// Runs the command.
        /// </summary>
        /// <typeparam name="T">The result type of the command.</typeparam>
        /// <param name="database">The database.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The result of the command.
        /// </returns>
        public static Task<T> RunCommandAsync<T>(this IMongoDatabase database, BsonDocument command, CancellationToken cancellationToken = default(CancellationToken))
        {
            return database.RunCommandAsync<T>(command, cancellationToken);
        }
    }
}