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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenEstimatedCountTest : JsonDrivenCollectionTest
    {
        // private fields
        private FilterDefinition<BsonDocument> _filter = new BsonDocument();
        private EstimatedDocumentCountOptions _options = new EstimatedDocumentCountOptions();
        private long _result;

        // public constructors
        public JsonDrivenEstimatedCountTest(IMongoCollection<BsonDocument> collection, Dictionary<string, object> objectMap)
            : base(collection, objectMap)
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "collectionOptions", "arguments", "result", "error");
            base.Arrange(document);
        }

        // protected methods
        protected override void AssertResult()
        {
            _result.Should().Be(_expectedResult.ToInt64());
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            _result = _collection.EstimatedDocumentCount(_options, cancellationToken);
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            _result = await _collection.EstimatedDocumentCountAsync(_options, cancellationToken).ConfigureAwait(false);
        }
    }
}
