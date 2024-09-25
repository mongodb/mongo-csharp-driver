/* Copyright 2010-present MongoDB Inc.
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

using Xunit;

namespace MongoDB.Driver.Tests.Specifications.crud.prose_tests
{
    public class BulkWriteProseTests
    {
        [Fact]
        public void Do()
        {
            var client = DriverTestConfiguration.Client;
            client.BulkWrite(new BulkWriteModel[]
            {
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection1",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection1",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection1",
                    new MyModel { A = 345 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection1",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection1",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection1",
                    new MyModel { A = 345 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection2",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection2",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection2",
                    new MyModel { A = 345 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection2",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection2",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection2",
                    new MyModel { A = 345 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 345 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection1",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection1",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection1",
                    new MyModel { A = 345 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 345 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 345 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection2",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection2",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection2",
                    new MyModel { A = 345 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 345 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 345 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 345 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 345 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 345 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 123 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 234 }),
                new BulkWriteInsertOneModel<MyModel>(
                    "testDb.TestCollection3",
                    new MyModel { A = 345 }),
                new BulkWriteDeleteManyModel<MyModel>(
                    "testDb.TestCollection1",
                    Builders<MyModel>.Filter.Eq(m => m.A, 123)),
                new BulkWriteDeleteManyModel<MyModel>(
                    "testDb.TestCollection1",
                    Builders<MyModel>.Filter.Eq(m => m.A, 234)),
                new BulkWriteDeleteManyModel<MyModel>(
                    "testDb.TestCollection1",
                    Builders<MyModel>.Filter.Eq(m => m.A, 345)),
                new BulkWriteDeleteManyModel<MyModel>(
                    "testDb.TestCollection2",
                    Builders<MyModel>.Filter.Eq(m => m.A, 123)),
                new BulkWriteDeleteManyModel<MyModel>(
                    "testDb.TestCollection2",
                    Builders<MyModel>.Filter.Eq(m => m.A, 234)),
                new BulkWriteDeleteManyModel<MyModel>(
                    "testDb.TestCollection2",
                    Builders<MyModel>.Filter.Eq(m => m.A, 345))
            });

            //
            // BulkWrite.InsertOne(CollectionNamespace.FromFullName("testDb.TestCollection"), new MyModel { A = 234 });
            // BulkWrite.InsertOne(CollectionNamespace.FromFullName("testDb.TestCollection"), new MyModel { A = 234 })
        }

        private class MyModel
        {
            public int A { get; set; }
        }
    }
}
