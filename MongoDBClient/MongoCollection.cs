/* Copyright 2010 10gen Inc.
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

using MongoDB.BsonLibrary;
using MongoDB.MongoDBClient.Internal;

namespace MongoDB.MongoDBClient {
    public class MongoCollection {
        #region protected fields
        protected MongoDatabase database;
        protected string name;
        #endregion

        #region constructors
        public MongoCollection(
            MongoDatabase database,
            string name
        ) {
            this.database = database;
            this.name = name;
        }
        #endregion

        #region public properties
        public MongoDatabase Database {
            get { return database; }
        }

        public string Name {
            get { return name; }
        }
        #endregion

        #region public methods
        public IEnumerable<T> FindAll<T>() where T : class, new() {
            MongoReplyMessage<T> reply;
            var client = database.Client;
            var connection = MongoConnectionPool.AcquireConnection(client.Host, client.Port);
            try {
                var message = new MongoQueryMessage(database, this);
                connection.SendMessage(message);
                reply = connection.ReceiveMessage<T>();
                MongoConnectionPool.ReleaseConnection(connection);
            } catch {
                try { connection.Dispose(); } catch { } // ignore exceptions
                throw;
            }

            if ((reply.ResponseFlags & ResponseFlags.QueryFailure) != 0) {
                throw new MongoException("Query failure");
            }
            return reply.Documents;
        }
        #endregion
    }

    public class MongoCollection<T> : MongoCollection where T : class, new() {
        #region constructors
        public MongoCollection(
            MongoDatabase database,
            string name
        )
            : base(database, name) {
        }
        #endregion

        #region public methods
        public IEnumerable<T> FindAll() {
            return FindAll<T>();
        }
        #endregion
    }
}
