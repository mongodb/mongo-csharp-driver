// /* Copyright 2010-present MongoDB Inc.
// *
// * Licensed under the Apache License, Version 2.0 (the "License");
// * you may not use this file except in compliance with the License.
// * You may obtain a copy of the License at
// *
// * http://www.apache.org/licenses/LICENSE-2.0
// *
// * Unless required by applicable law or agreed to in writing, software
// * distributed under the License is distributed on an "AS IS" BASIS,
// * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// * See the License for the specific language governing permissions and
// * limitations under the License.
// */
//
// using FluentAssertions;
// using MongoDB.Bson;
// using MongoDB.Bson.Serialization;
// using MongoDB.Bson.Serialization.Attributes;
// using Xunit;
//
// namespace MongoDB.Driver.Tests.Specifications.crud.prose_tests
// {
//     public class BulkWriteProseTests
//     {
//         public BulkWriteProseTests()
//         {
//             var client = DriverTestConfiguration.Client;
//             client.ListDatabases();
//             BsonSerializer.LookupSerializer<MyModel>();
//
//             ClientBulkWrite();
//             CollectionBulkWrite();
//         }
//
//         [Fact]
//         public void ClientBulkWrite()
//         {
//             var client = DriverTestConfiguration.Client;
//             var writes = new BulkWriteModel[]
//             {
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }), //, Id = ObjectId.Parse("66fc7e9fb54f11a4e257b907")}),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 123 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 234 }),
//                 new BulkWriteInsertOneModel<MyModel>(
//                     "testDb.TestCollection1",
//                     new MyModel { A = 345 })
//             };
//
//             var result = client.BulkWrite(writes, new ClientBulkWriteOptions { VerboseResult = true, });
//             result.InsertedCount.Should().Be(45);
//         }
//
//         [Fact]
//         public void CollectionBulkWrite()
//         {
//             var client = DriverTestConfiguration.Client;
//             var collection = client.GetDatabase("testDb").GetCollection<MyModel>("TestCollection2");
//
//             var writes = new WriteModel<MyModel>[]
//             {
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 123 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 234 }),
//                 new InsertOneModel<MyModel>(new MyModel { A = 345 })
//             };
//
//             var result = collection.BulkWrite(writes);
//
//             result.InsertedCount.Should().Be(45);
//         }
//
//         private class MyModel
//         {
//             public int A { get; set; }
//
//             [BsonId]
//             public ObjectId Id { get; set; }
//         }
//     }
// }
