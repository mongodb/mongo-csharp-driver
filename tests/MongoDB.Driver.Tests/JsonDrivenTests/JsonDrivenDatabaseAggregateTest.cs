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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public class JsonDrivenDatabaseAggregateTest : JsonDrivenDatabaseTest
    {
        // private fields
        private AggregateOptions _options = new AggregateOptions();
        private PipelineDefinition<NoPipelineInput, BsonDocument> _pipeline;
        private List<BsonDocument> _result;
        private IClientSessionHandle _session;

        public JsonDrivenDatabaseAggregateTest(IMongoDatabase database, Dictionary<string, object> objectMap) : base(database, objectMap)
        {
        }

        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "arguments", "result", "error");
            base.Arrange(document);
        }

        // protected methods
        protected override void AssertResult()
        {
            _result.Should().Equal(_expectedResult.AsBsonArray.Cast<BsonDocument>());
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            IAsyncCursor<BsonDocument> cursor;
            if (_session == null)
            {
                cursor = _database.Aggregate(_pipeline, _options, cancellationToken);
            }
            else
            {
                cursor = _database.Aggregate(_session, _pipeline, _options, cancellationToken);
            }
            _result = cursor.ToList();
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            IAsyncCursor<BsonDocument> cursor;
            if (_session == null)
            {
                cursor = await _database.AggregateAsync(_pipeline, _options, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                cursor = await _database.AggregateAsync(_session, _pipeline, _options, cancellationToken).ConfigureAwait(false);
            }
            _result = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "batchSize":
                    _options.BatchSize = value.ToInt32();
                    return;

                case "pipeline":
                    _pipeline = new BsonDocumentStagePipelineDefinition<NoPipelineInput, BsonDocument>(value.AsBsonArray.Cast<BsonDocument>());
                    return;

                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
