/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a client to MongoDB.
    /// </summary>
    public class MongoClient
    {
        // private fields
        private readonly MongoClientSettings _settings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        public MongoClient()
            : this(new MongoClientSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public MongoClient(MongoClientSettings settings)
        {
            _settings = settings.FrozenCopy();
        }

        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        /// <param name="url">The URL.</param>
        public MongoClient(MongoUrl url)
            : this(MongoClientSettings.FromUrl(url))
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoClient class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public MongoClient(string connectionString)
            : this(ParseConnectionString(connectionString))
        {
        }

        // public properties
        /// <summary>
        /// Gets the client settings.
        /// </summary>
        public MongoClientSettings Settings
        {
            get { return _settings; }
        }

        // private static methods
        private static MongoClientSettings ParseConnectionString(string connectionString)
        {
            if (connectionString.StartsWith("mongodb://"))
            {
                var url = new MongoUrl(connectionString);
                return MongoClientSettings.FromUrl(url);
            }
            else
            {
                var builder = new MongoConnectionStringBuilder(connectionString);
                return MongoClientSettings.FromConnectionStringBuilder(builder);
            }
        }

        // public methods
        /// <summary>
        /// Gets a MongoDatabase instance representing a database. See also GetServer.
        /// </summary>
        /// <param name="databaseSettings">The settings to use with this database.</param>
        /// <returns>An instance of MongoDatabase.</returns>
        public MongoDatabase GetDatabase(MongoDatabaseSettings databaseSettings)
        {
            return GetServer().GetDatabase(databaseSettings);
        }

        /// <summary>
        /// Gets a MongoDatabase instance representing a database. See also GetServer.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>An instance of MongoDatabase.</returns>
        public virtual MongoDatabase GetDatabase(string databaseName)
        {
            return GetServer().GetDatabase(databaseName);
        }

        /// <summary>
        /// Gets a MongoServer object using this client's settings. See also GetDatabase.
        /// </summary>
        /// <returns>A MongoServer.</returns>
        public MongoServer GetServer()
        {
            var serverSettings = MongoServerSettings.FromClientSettings(_settings);
#pragma warning disable 618
            return MongoServer.Create(serverSettings);
#pragma warning restore
        }
    }
}
