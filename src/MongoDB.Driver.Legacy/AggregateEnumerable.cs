/* Copyright 2010-2015 MongoDB Inc.
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

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents an AggregateOperation whose execution is deferred until GetEnumerator is called.
    /// </summary>
    internal class AggregateEnumerable : IEnumerable<BsonDocument>
    {
        // fields
        private readonly MongoCollection _collection;
        private readonly IReadOperation<IAsyncCursor<BsonDocument>> _operation;
        private readonly ReadPreference _readPreference;

        // constructors
        public AggregateEnumerable(MongoCollection collection, IReadOperation<IAsyncCursor<BsonDocument>> operation, ReadPreference readPreference)
        {
            _collection = collection;
            _operation = operation;
            _readPreference = readPreference;
        }

        // methods
        public IEnumerator<BsonDocument> GetEnumerator()
        {
            var cursor = _collection.ExecuteReadOperation(_operation, _readPreference);
            return cursor.ToEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
