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

namespace MongoDB.Driver.Core.Operations
{
    public class DeleteRequest
    {
        // fields
        private int _limit;
        private BsonDocument _query;

        // constructors
        public DeleteRequest(BsonDocument query, int limit = 1)
        {
            _query = Ensure.IsNotNull(query, "query");
            _limit = limit;
        }

        // properties
        public int Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        public BsonDocument Query
        {
            get { return _query; }
            set { _query = value; }
        }
    }
}
