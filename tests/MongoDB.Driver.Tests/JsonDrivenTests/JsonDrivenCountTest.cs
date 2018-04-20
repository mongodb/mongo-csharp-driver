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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenCountTest : JsonDrivenCollectionTest
    {
        // private fields
        private FilterDefinition<BsonDocument> _filter = new BsonDocument();
        private CountOptions _options = new CountOptions();
        private long _result;

        // public constructors
        public JsonDrivenCountTest(IMongoClient client, IMongoDatabase database, IMongoCollection<BsonDocument> collection, Dictionary<string, IClientSessionHandle> sessionMap)
            : base(client, database, collection, sessionMap)
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "arguments", "result");
            base.Arrange(document);
        }

        // protected methods
        protected override void AssertResult()
        {
            _result.Should().Be(_expectedResult.ToInt64());
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            _result = _collection.Count(_filter, _options, cancellationToken);
        }

        protected override void CallMethod(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            _result = _collection.Count(session, _filter, _options, cancellationToken);
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            _result = await _collection.CountAsync(_filter, _options, cancellationToken);
        }

        protected override async Task CallMethodAsync(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            _result = await _collection.CountAsync(session, _filter, _options, cancellationToken);
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "filter":
                    _filter = new BsonDocumentFilterDefinition<BsonDocument>(value.AsBsonDocument);
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
