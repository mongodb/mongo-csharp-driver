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

using System;
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
        private ReadConcern _readConcern = new ReadConcern();
        private ReadPreference _readPreference;
        private BsonDocument _result;
        private IClientSessionHandle _session;

        // public constructors
        public JsonDrivenRunCommandTest(IMongoDatabase database, Dictionary<string, object> objectMap)
            : base(database, objectMap)
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            var expectedNames = new[] { "name", "object", "command_name", "arguments", "result", "databaseOptions" };
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, expectedNames);
            base.Arrange(document);

            if (document.Contains("command_name"))
            {
                var actualCommandName = _command.GetElement(0).Name;
                var expectedCommandName = document["command_name"].AsString;
                if (actualCommandName != expectedCommandName)
                {
                    throw new FormatException($"Actual command name \"{actualCommandName}\" does not match expected command name \"{expectedCommandName}\".");
                }
            }
        }

        // protected methods
        protected override void AssertResult()
        {
            var aspectAsserter = new BsonDocumentAspectAsserter();
            aspectAsserter.AssertAspects(_result, _expectedResult.AsBsonDocument);
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                _result = _database
                    .WithReadConcern(_readConcern)
                    .RunCommand<BsonDocument>(_command, _readPreference, cancellationToken);
            }
            else
            {
                _result = _database
                    .WithReadConcern(_readConcern)
                    .RunCommand<BsonDocument>(_session, _command, _readPreference, cancellationToken);
            }
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                _result = await _database
                    .WithReadConcern(_readConcern)
                    .RunCommandAsync<BsonDocument>(_command, _readPreference, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _result = await _database
                    .WithReadConcern(_readConcern)
                    .RunCommandAsync<BsonDocument>(_session, _command, _readPreference, cancellationToken).ConfigureAwait(false);
            }
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "command":
                    _command = value.AsBsonDocument;
                    return;

                case "databaseOptions":
                    if (value.AsBsonDocument.TryGetValue("readConcern", out var readConcernValue))
                    {
                        _readConcern = ReadConcern.FromBsonDocument(readConcernValue.AsBsonDocument);
                    }
                    return;

                case "readPreference":
                    _readPreference = ReadPreference.FromBsonDocument(value.AsBsonDocument);
                    return;

                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
