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
        #region private fields
        private MongoDatabase database;
        private string name;
        #endregion

        #region constructors
        internal MongoCollection(
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
        public IEnumerable<BsonDocument> Find() {
            MongoClient client = database.Client;
            MongoConnection connection = MongoConnectionPool.AcquireConnection(client.Host, client.Port);
            try {
                MongoQueryMessage query = new MongoQueryMessage(database, this);
                query.NumberToSkip = 0;
                query.NumberToReturn = 100;
                connection.SendMessage(query);

                MongoReplyMessage reply = connection.ReceiveMessage();
                MongoConnectionPool.ReleaseConnection(connection);
                if ((reply.ResponseFlags & ResponseFlags.QueryFailure) != 0) {
                    throw new MongoException("Query failure");
                }
                return reply.Documents;
            } catch {
                try { connection.Dispose(); } catch { } // ignore exceptions
                throw;
            }
        }
        #endregion
    }
}
