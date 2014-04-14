/* Copyright 2010-2014 MongoDB Inc.
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
using System.Collections.ObjectModel;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Operations;
using MongoDB.Driver.Support;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Operations
{
    [TestFixture]
    public class BulkWriteBatchResultCombinerTests
    {
        private static readonly ReadOnlyCollection<WriteRequest> __noProcessedRequests = new ReadOnlyCollection<WriteRequest>(new WriteRequest[0]);
        private static readonly ReadOnlyCollection<BulkWriteUpsert> __noUpserts = new ReadOnlyCollection<BulkWriteUpsert>(new BulkWriteUpsert[0]);

        [Test]
        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(0, 2)]
        [TestCase(3, 4)]
        public void TestCombineDeletedCount(long deletedCount1, long deletedCount2)
        {
            var result1 = CreateBatchResult(deletedCount: deletedCount1);
            var result2 = CreateBatchResult(deletedCount: deletedCount2);
            var result = CombineResults(result1, result2);
            Assert.AreEqual(deletedCount1 + deletedCount2, result.DeletedCount);
            Assert.AreEqual(0, result.InsertedCount);
            Assert.AreEqual(0, result.ModifiedCount);
            Assert.AreEqual(0, result.MatchedCount);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(0, 2)]
        [TestCase(3, 4)]
        public void TestCombineInsertedCount(long insertedCount1, long insertedCount2)
        {
            var result1 = CreateBatchResult(insertedCount: insertedCount1);
            var result2 = CreateBatchResult(insertedCount: insertedCount2);
            var result = CombineResults(result1, result2);
            Assert.AreEqual(insertedCount1 + insertedCount2, result.InsertedCount);
            Assert.AreEqual(0, result.DeletedCount);
            Assert.AreEqual(0, result.ModifiedCount);
            Assert.AreEqual(0, result.MatchedCount);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(0, 2)]
        [TestCase(3, 4)]
        public void TestCombineMatchedCount(long matchedCount1, long matchedCount2)
        {
            var result1 = CreateBatchResult(matchedCount: matchedCount1);
            var result2 = CreateBatchResult(matchedCount: matchedCount2);
            var result = CombineResults(result1, result2);
            Assert.AreEqual(matchedCount1 + matchedCount2, result.MatchedCount);
            Assert.AreEqual(0, result.DeletedCount);
            Assert.AreEqual(0, result.InsertedCount);
            Assert.AreEqual(0, result.ModifiedCount);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(0, 2)]
        [TestCase(3, 4)]
        public void TestCombineModifiedCount(long modifiedCount1, long modifiedCount2)
        {
            var result1 = CreateBatchResult(modifiedCount: modifiedCount1);
            var result2 = CreateBatchResult(modifiedCount: modifiedCount2);
            var result = CombineResults(result1, result2);
            Assert.AreEqual(modifiedCount1 + modifiedCount2, result.ModifiedCount);
            Assert.AreEqual(0, result.DeletedCount);
            Assert.AreEqual(0, result.InsertedCount);
            Assert.AreEqual(0, result.MatchedCount);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(0, 2)]
        [TestCase(3, 4)]
        public void TestCombineProcessedRequests(int count1, int count2)
        {
            var processedRequests1 = Enumerable.Range(0, count1).Select(i => (WriteRequest)new InsertRequest(typeof(BsonDocument), new BsonDocument("_id", i))).ToList();
            var processedRequests2 = Enumerable.Range(0, count2).Select(i => (WriteRequest)new InsertRequest(typeof(BsonDocument), new BsonDocument("_id", i))).ToList();
            var result1 = CreateBatchResult(processedRequests: processedRequests1);
            var result2 = CreateBatchResult(processedRequests: processedRequests2);
            var result = CombineResults(result1, result2);
            var expectedProcessedRequests = processedRequests1.Concat(processedRequests2);
            Assert.IsTrue(expectedProcessedRequests.SequenceEqual(result.ProcessedRequests));
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(0, 2)]
        [TestCase(3, 4)]
        public void TestCombineUpserts(int count1, int count2)
        {
            var upserts1 = Enumerable.Range(0, count1).Select(i => new BulkWriteUpsert(i, i)).ToList();
            var upserts2 = Enumerable.Range(count1, count1 + count2).Select(i => new BulkWriteUpsert(i, i)).ToList();
            var result1 = CreateBatchResult(upserts: upserts1);
            var result2 = CreateBatchResult(upserts: upserts2);
            var result = CombineResults(result1, result2);
            var expectedUpserts = upserts1.Concat(upserts2).ToArray();
            Assert.IsTrue(expectedUpserts.SequenceEqual(result.Upserts, new BulkWriteUpsertComparer()));
        }

        private BulkWriteResult CombineResults(params BulkWriteBatchResult[] batchResults)
        {
            var combiner = new BulkWriteBatchResultCombiner(batchResults, true);
            return combiner.CreateResultOrThrowIfHasErrors(Enumerable.Empty<WriteRequest>());
        }

        private BulkWriteBatchResult CreateBatchResult(
            int? batchCount = null,
            int? requestCount = null,
            long? matchedCount = null,
            long? deletedCount = null,
            long? insertedCount = null,
            long? modifiedCount = null,
            IEnumerable<WriteRequest> processedRequests = null,
            IEnumerable<WriteRequest> unprocessedRequests = null,
            IEnumerable<BulkWriteUpsert> upserts = null,
            IEnumerable<BulkWriteError> writeErrors = null,
            WriteConcernError writeConcernError = null,
            IndexMap indexMap = null,
            Batch<WriteRequest> nextBatch = null)            
        {
            return new BulkWriteBatchResult(
                batchCount ?? 1,
                processedRequests ?? Enumerable.Empty<WriteRequest>(),
                unprocessedRequests ?? Enumerable.Empty<WriteRequest>(),
                matchedCount ?? 0,
                deletedCount ?? 0,
                insertedCount ?? 0,
                modifiedCount ?? 0,
                upserts ?? Enumerable.Empty<BulkWriteUpsert>(),
                writeErrors ?? Enumerable.Empty<BulkWriteError>(),
                writeConcernError,
                indexMap ?? IndexMap.IdentityMap,
                nextBatch);
        }

        // nested types
        private class BulkWriteUpsertComparer : IEqualityComparer<BulkWriteUpsert>
        {
            public bool Equals(BulkWriteUpsert lhs, BulkWriteUpsert rhs)
            {
                if (object.ReferenceEquals(lhs, rhs)) { return true; }
                if (object.ReferenceEquals(lhs, null) || object.ReferenceEquals(rhs, null)) { return false; }
                return lhs.Index == rhs.Index && lhs.Id == rhs.Id;
            }

            public int GetHashCode(BulkWriteUpsert obj)
            {
                return 0;
            }
        }
    }
}
