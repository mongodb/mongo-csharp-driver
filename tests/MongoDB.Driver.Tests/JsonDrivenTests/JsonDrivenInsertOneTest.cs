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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenInsertOneTest : JsonDrivenCollectionTest
    {
        // private fields
        private BsonDocument _document;
        private InsertOneOptions _options = new InsertOneOptions();

        // public constructors
        public JsonDrivenInsertOneTest(IMongoClient client, IMongoDatabase database, IMongoCollection<BsonDocument> collection, Dictionary<string, IClientSessionHandle> sessionMap)
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
            // test has a "result" but the InsertOne method returns void in the C# driver
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            _collection.InsertOne(_document, _options, cancellationToken);
        }

        protected override void CallMethod(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            _collection.InsertOne(session, _document, _options, cancellationToken);
        }

        protected override Task CallMethodAsync(CancellationToken cancellationToken)
        {
            return _collection.InsertOneAsync(_document, _options, cancellationToken);
        }

        protected override Task CallMethodAsync(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            return _collection.InsertOneAsync(session, _document, _options, cancellationToken);
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "document":
                    _document = value.AsBsonDocument;
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
