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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class InsertManyTest : CrudOperationTestBase
    {
        private List<BsonDocument> _documents;

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "documents":
                    _documents = ((BsonArray)value).Select(x => (BsonDocument)x).ToList();
                    return true;
            }

            return false;
        }

        protected override void Execute(IMongoCollection<BsonDocument> collection, BsonDocument outcome, bool async)
        {
            if (async)
            {
                collection.InsertManyAsync(_documents).GetAwaiter().GetResult();
            }
            else
            {
                collection.InsertMany(_documents);
            }
        }
    }
}
