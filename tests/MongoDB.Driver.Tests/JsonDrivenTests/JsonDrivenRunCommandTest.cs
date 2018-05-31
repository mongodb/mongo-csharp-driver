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
    public sealed class JsonDrivenRunCommandTest : JsonDrivenDatabaseTest
    {
        // private fields
        private BsonDocument _command;
        private ReadPreference _readPreference;
        private BsonDocument _result;

        // public constructors
        public JsonDrivenRunCommandTest(IMongoClient client, IMongoDatabase database, Dictionary<string, IClientSessionHandle> sessionMap)
            : base(client, database, sessionMap)
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
            var aspectAsserter = new BsonDocumentAspectAsserter();
            aspectAsserter.AssertAspects(_result, _expectedResult.AsBsonDocument);
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            _result = _database.RunCommand<BsonDocument>(_command, _readPreference, cancellationToken);
        }

        protected override void CallMethod(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            _result = _database.RunCommand<BsonDocument>(session, _command, _readPreference, cancellationToken);
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            _result = await _database.RunCommandAsync<BsonDocument>(_command, _readPreference, cancellationToken);
        }

        protected override async Task CallMethodAsync(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            _result = await _database.RunCommandAsync<BsonDocument>(session, _command, _readPreference, cancellationToken);
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "command":
                    _command = value.AsBsonDocument;
                    return;

                case "readPreference":
                    _readPreference = ReadPreference.FromBsonDocument(value.AsBsonDocument);
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
