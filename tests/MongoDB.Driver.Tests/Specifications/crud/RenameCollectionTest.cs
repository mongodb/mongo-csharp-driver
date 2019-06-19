/* Copyright 2019-present MongoDB Inc.
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

using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class RenameCollectionTest : CrudOperationTestBase
    {
        public string _to;

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "to":
                    _to = value.AsString;
                    return true;
            }

            return false;
        }

        protected override void Execute(IMongoCollection<BsonDocument> collection, BsonDocument outcome, bool async)
        {
            var database = collection.Database;
            var oldName = collection.CollectionNamespace.CollectionName;
            if (async)
            {
                database.RenameCollectionAsync(oldName, _to).GetAwaiter().GetResult();
            }
            else
            {
                database.RenameCollection(oldName, _to);
            }
        }
    }
}
