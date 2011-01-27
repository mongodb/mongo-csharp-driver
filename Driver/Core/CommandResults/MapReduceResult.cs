/* Copyright 2010-2011 10gen Inc.
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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver {
    public class MapReduceResult : CommandResult {
        #region constructors
        public MapReduceResult() {
        }
        #endregion

        #region public properties
        public string CollectionName {
            get { return (string) response["result", null]; }
        }

        public TimeSpan Duration {
            get { return TimeSpan.FromMilliseconds(response["timeMillis"].ToInt32()); }
        }

        public int EmitCount {
            get { return response["counts"].AsBsonDocument["emit"].ToInt32(); }
        }

        public int OutputCount {
            get { return response["counts"].AsBsonDocument["output"].ToInt32(); }
        }

        public IEnumerable<BsonDocument> InlineResults {
            get { return response["results"].AsBsonArray.Cast<BsonDocument>(); }
        }

        public int InputCount {
            get { return response["counts"].AsBsonDocument["input"].ToInt32(); }
        }
        #endregion

        #region public methods
        public IEnumerable<TDocument> GetInlineResultsAs<TDocument>() {
            return InlineResults.Select(document => BsonSerializer.Deserialize<TDocument>(document));
        }
        #endregion
    }
}
