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
    public sealed class JsonDrivenCountDocumentsTest : JsonDrivenCollectionTest
    {
        // private fields
        private FilterDefinition<BsonDocument> _filter = new BsonDocument();
        private CountOptions _options = new CountOptions();
        private long _result;
        private IClientSessionHandle _session;

        // public constructors
        public JsonDrivenCountDocumentsTest(IMongoCollection<BsonDocument> collection, Dictionary<string, object> objectMap)
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
            if (_session == null)
            {
#pragma warning disable 618
                _result = _collection.CountDocuments(_filter, _options, cancellationToken);
#pragma warning restore
            }
            else
            {
#pragma warning disable 618
                _result = _collection.CountDocuments(_session, _filter, _options, cancellationToken);
#pragma warning restore
            }
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
#pragma warning disable 618
                _result = await _collection.CountDocumentsAsync(_filter, _options, cancellationToken).ConfigureAwait(false);
#pragma warning restore
            }
            else
            {
#pragma warning disable 618
                _result = await _collection.CountDocumentsAsync(_session, _filter, _options, cancellationToken).ConfigureAwait(false);
#pragma warning restore
            }
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "filter":
                    _filter = new BsonDocumentFilterDefinition<BsonDocument>(value.AsBsonDocument);
                    return;

                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
