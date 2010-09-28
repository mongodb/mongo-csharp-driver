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

namespace MongoDB.CSharpDriver {
    public class MongoMapReduceResult {
        #region private fields
        private MongoDatabase database;
        private BsonDocument commandResult;
        #endregion

        #region constructors
        public MongoMapReduceResult(
            MongoDatabase database,
            BsonDocument commandResult
        ) {
            this.database = database;
            this.commandResult = commandResult;
        }
        #endregion

        #region public properties
        public BsonDocument CommandResult {
            get { return commandResult; }
        }

        public BsonDocument Counts {
            get { return commandResult["counts"].AsBsonDocument; }
        }

        public MongoDatabase Database {
            get { return database; }
        }

        public TimeSpan Duration {
            get { return TimeSpan.FromMilliseconds(commandResult["timeMillis"].AsDouble); }
        }

        public string ResultCollectionName {
            get { return commandResult["result"].AsString; }
        }
        #endregion

        #region public methods
        public MongoCursor<BsonDocument, R> GetResults<R>() where R : new() {
            MongoCollection<R> collection = database.GetCollection<R>(ResultCollectionName);
            return collection.FindAll();
        }
        #endregion
    }
}
