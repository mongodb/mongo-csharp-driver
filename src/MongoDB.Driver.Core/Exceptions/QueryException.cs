/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Exceptions
{
    public class QueryException : MongoDBException
    {
        // fields
        private readonly BsonDocument _query;
        private readonly BsonDocument _result;

        // constructors
        public QueryException(string message, BsonDocument query)
            : this(message, query, null, null)
        {
        }

        public QueryException(string message, BsonDocument query, BsonDocument result)
            : this(message, query, result, null)
        {
        }

        public QueryException(string message, BsonDocument query, BsonDocument result, Exception innerException)
            : base(message, innerException)
        {
            _query = Ensure.IsNotNull(query, "query");
            _result = result; // can be null
        }

        // properties
        public BsonDocument Query
        {
            get { return _query; }
        }

        public BsonDocument Result
        {
            get { return _result; }
        }
    }
}
