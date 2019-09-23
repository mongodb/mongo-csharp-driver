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
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenInsertOneTest : JsonDrivenCollectionTest
    {
        // private fields
        private BsonDocument _document;
        private InsertOneOptions _options = new InsertOneOptions();
        private IClientSessionHandle _session;

        // public constructors
        public JsonDrivenInsertOneTest(IMongoCollection<BsonDocument> collection, Dictionary<string, object> objectMap)
            : base(collection, objectMap)
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "collectionOptions", "arguments", "result");
            base.Arrange(document);
        }

        // protected methods
        protected override void AssertResult()
        {
            // test has a "result" but the InsertOne method returns void in the C# driver
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                _collection.InsertOne(_document, _options, cancellationToken);
            }
            else
            {
                _collection.InsertOne(_session, _document, _options, cancellationToken);
            }
        }

        protected override Task CallMethodAsync(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                return _collection.InsertOneAsync(_document, _options, cancellationToken);
            }
            else
            {
                return _collection.InsertOneAsync(_session, _document, _options, cancellationToken);
            }
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "bypassDocumentValidation":
                    _options.BypassDocumentValidation = value.ToBoolean();
                    return;

                case "document":
                    _document = value.AsBsonDocument;
                    return;

                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
