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
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenInsertManyTest : JsonDrivenCollectionTest
    {
        // private fields
        private List<BsonDocument> _documents;
        private InsertManyOptions _options = new InsertManyOptions();
        private IClientSessionHandle _session;

        // public constructors
        public JsonDrivenInsertManyTest(IMongoCollection<BsonDocument> collection, Dictionary<string, object> objectMap)
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
            // test has a "result" but the InsertMany method returns void in the C# driver
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                _collection.InsertMany(_documents, _options, cancellationToken);
            }
            else
            {
                _collection.InsertMany(_session, _documents, _options, cancellationToken);
            }
        }

        protected override Task CallMethodAsync(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                return _collection.InsertManyAsync(_documents, _options, cancellationToken);
            }
            else
            {
                return _collection.InsertManyAsync(_session, _documents, _options, cancellationToken);
            }
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "documents":
                    _documents = value.AsBsonArray.Cast<BsonDocument>().ToList();
                    return;

                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
