/* Copyright 2018-present MongoDB Inc.
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

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public static class CrudOperationTestFactory
    {
        public static ICrudOperationTest CreateTest(string name)
        {
            switch (name)
            {
                case "aggregate": return new AggregateTest();
                case "bulkWrite": return new BulkWriteTest();
                case "count": return new CountTest();
                case "deleteMany": return new DeleteManyTest();
                case "deleteOne": return new DeleteOneTest();
                case "distinct": return new DistinctTest();
                case "drop": return new DropCollectionTest();
                case "find": return new FindTest();
                case "findOneAndDelete": return new FindOneAndDeleteTest();
                case "findOneAndReplace": return new FindOneAndReplaceTest();
                case "findOneAndUpdate": return new FindOneAndUpdateTest();
                case "insertMany": return new InsertManyTest();
                case "insertOne": return new InsertOneTest();
                case "rename": return new RenameCollectionTest();
                case "replaceOne": return new ReplaceOneTest();
                case "updateOne": return new UpdateOneTest();
                case "updateMany": return new UpdateManyTest();
                default: throw new ArgumentException($"Invalid CRUD operation name: \"{name}\".");
            }
        }
    }
}
