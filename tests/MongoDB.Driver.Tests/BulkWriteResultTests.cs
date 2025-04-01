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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using MongoDB.Bson;
using MongoDB.Driver.Core.Operations;
using MongoDB.Bson.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class BulkWriteResultTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData((long)5)]
        public void Should_convert_from_core_acknowledged_result_when_original_models_exists(long? modifiedCount)
        {
            var core = new BulkWriteOperationResult.Acknowledged(
                requestCount: 1,
                matchedCount: 2,
                deletedCount: 3,
                insertedCount: 4,
                modifiedCount: modifiedCount,
                processedRequests: new[] { new InsertRequest(new BsonDocument("b", 1)) },
                upserts: new List<BulkWriteOperationUpsert>());

            var models = new[]
            {
                new InsertOneModel<BsonDocument>(new BsonDocument("a", 1))
            };
            var mapped = BulkWriteResult<BsonDocument>.FromCore(core, models);

            mapped.ProcessedRequests[0].ShouldBeSameAs(models[0]);

            mapped.IsAcknowledged.ShouldBeTrue();
            mapped.RequestCount.ShouldBe(core.RequestCount);
            mapped.MatchedCount.ShouldBe(core.MatchedCount);
            mapped.DeletedCount.ShouldBe(core.DeletedCount);
            mapped.InsertedCount.ShouldBe(core.InsertedCount);
            mapped.IsModifiedCountAvailable.ShouldBe(core.IsModifiedCountAvailable);
            if (mapped.IsModifiedCountAvailable)
            {
                mapped.ModifiedCount.ShouldBe(core.ModifiedCount);
            }
            mapped.Upserts.Count.ShouldBe(core.Upserts.Count);
        }

        [Fact]
        public void Should_convert_from_core_unacknowledged_result_when_original_models_exists()
        {
            var core = new BulkWriteOperationResult.Unacknowledged(
                requestCount: 1,
                processedRequests: new[] { new InsertRequest(new BsonDocument("b", 1)) });

            var models = new[]
            {
                new InsertOneModel<BsonDocument>(new BsonDocument("a", 1))
            };
            var mapped = BulkWriteResult<BsonDocument>.FromCore(core, models);

            mapped.ProcessedRequests[0].ShouldBeSameAs(models[0]);
            mapped.IsAcknowledged.ShouldBeFalse();
            mapped.RequestCount.ShouldBe(core.RequestCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData((long)5)]
        public void Should_convert_from_core_acknowledged_result_when_original_models_do_not_exist(long? modifiedCount)
        {
            var core = new BulkWriteOperationResult.Acknowledged(
                requestCount: 1,
                matchedCount: 2,
                deletedCount: 3,
                insertedCount: 4,
                modifiedCount: modifiedCount,
                processedRequests: new[] { new InsertRequest(new BsonDocumentWrapper(new BsonDocument("b", 1))) },
                upserts: new List<BulkWriteOperationUpsert>());

            var mapped = BulkWriteResult<BsonDocument>.FromCore(core);

            mapped.ProcessedRequests[0].ShouldBeOfType<InsertOneModel<BsonDocument>>();

            mapped.IsAcknowledged.ShouldBeTrue();
            mapped.RequestCount.ShouldBe(core.RequestCount);
            mapped.MatchedCount.ShouldBe(core.MatchedCount);
            mapped.DeletedCount.ShouldBe(core.DeletedCount);
            mapped.InsertedCount.ShouldBe(core.InsertedCount);
            mapped.IsModifiedCountAvailable.ShouldBe(core.IsModifiedCountAvailable);
            if (mapped.IsModifiedCountAvailable)
            {
                mapped.ModifiedCount.ShouldBe(core.ModifiedCount);
            }
            mapped.Upserts.Count.ShouldBe(core.Upserts.Count);
        }

        [Fact]
        public void Should_convert_from_core_unacknowledged_result_when_original_models_does_not_exist()
        {
            var core = new BulkWriteOperationResult.Unacknowledged(
                requestCount: 1,
                processedRequests: new[] { new InsertRequest(new BsonDocumentWrapper(new BsonDocument("b", 1))) });

            var mapped = BulkWriteResult<BsonDocument>.FromCore(core);

            mapped.ProcessedRequests[0].ShouldBeOfType<InsertOneModel<BsonDocument>>();
            ((InsertOneModel<BsonDocument>)mapped.ProcessedRequests[0]).Document.ShouldBe("{b:1}");
            mapped.IsAcknowledged.ShouldBeFalse();
            mapped.RequestCount.ShouldBe(core.RequestCount);
        }
    }
}
