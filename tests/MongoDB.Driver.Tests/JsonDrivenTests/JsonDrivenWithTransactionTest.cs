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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenWithTransactionTest : JsonDrivenSessionTest
    {
        // private fields
        private Func<IClientSessionHandle, CancellationToken, BsonDocument> _callback = null;
        private Func<IClientSessionHandle, CancellationToken, Task<BsonDocument>> _callbackAsync = null;
        private readonly JsonDrivenTestFactory _jsonDrivenTestFactory;
        private BsonDocument _result;
        private TransactionOptions _transactionOptions = null;

        // public constructors
        public JsonDrivenWithTransactionTest(JsonDrivenTestFactory jsonDrivenTestFactory, Dictionary<string, object> objectMap)
            : base(objectMap)
        {
            _jsonDrivenTestFactory = jsonDrivenTestFactory;
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "arguments", "result");

            base.Arrange(document);
        }

        // protected methods
        protected override void AssertResult()
        {
            _result.Should().ShouldBeEquivalentTo(_expectedResult.AsBsonDocument);
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            _result = _session.WithTransaction(_callback, _transactionOptions, cancellationToken);
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            _result = await _session.WithTransactionAsync(_callbackAsync, _transactionOptions, cancellationToken).ConfigureAwait(false);
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "options":
                    _transactionOptions = ParseTransactionOptions(value.AsBsonDocument);
                    return;
                case "callback":
                    {
                        var valueDocument = value.AsBsonDocument;
                        JsonDrivenHelper.EnsureAllFieldsAreValid(valueDocument, "operations");
                        var operations = valueDocument["operations"].AsBsonArray;
                        _callback = ParseCallback(operations);
                        _callbackAsync = ParseCallbackAsync(operations);
                    }
                    return;
            }

            base.SetArgument(name, value);
        }

        // private methods
        private TransactionOptions ParseTransactionOptions(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "readConcern", "readPreference", "writeConcern", "maxCommitTimeMS");

            var options = new TransactionOptions();
            if (document.Contains("readConcern"))
            {
                options = options.With(readConcern: ReadConcern.FromBsonDocument(document["readConcern"].AsBsonDocument));
            }

            if (document.Contains("readPreference"))
            {
                options = options.With(readPreference: ReadPreference.FromBsonDocument(document["readPreference"].AsBsonDocument));
            }

            if (document.Contains("writeConcern"))
            {
                options = options.With(writeConcern: WriteConcern.FromBsonDocument(document["writeConcern"].AsBsonDocument));
            }

            if (document.Contains("maxCommitTimeMS"))
            {
                options = options.With(maxCommitTime: TimeSpan.FromMilliseconds(document["maxCommitTimeMS"].ToInt32()));
            }

            return options;
        }

        private Func<IClientSessionHandle, CancellationToken, BsonDocument> ParseCallback(BsonArray operations)
        {
            return (session, cancellationToken) =>
            {
                session.Should().BeSameAs(_session);

                var tests = ParseCallbackOperations(operations); // each invocation of the callback needs a new copy of the tests
                foreach (var test in tests)
                {
                    test.Act(cancellationToken); // uses the session from the JSON file which usually is the same as the one passed to the callback
                    test.ThrowActualExceptionIfNotNull();
                }
                return null;
            };
        }

        private Func<IClientSessionHandle, CancellationToken, Task<BsonDocument>> ParseCallbackAsync(BsonArray operations)
        {
            return async (session, cancellationToken) =>
            {
                session.Should().BeSameAs(_session);

                var tests = ParseCallbackOperations(operations); // each invocation of the callback needs a new copy of the tests
                foreach (var test in tests)
                {
                    await test.ActAsync(cancellationToken).ConfigureAwait(false); // uses the session from the JSON file which usually is the same as the one passed to the callback
                    test.ThrowActualExceptionIfNotNull();
                }
                return null;
            };
        }

        private List<JsonDrivenTest> ParseCallbackOperations(BsonArray operations)
        {
            var tests = new List<JsonDrivenTest>();
            foreach (var operation in operations.Cast<BsonDocument>())
            {
                var methodName = operation["name"].AsString;
                var @object = operation["object"].AsString;
                var test = _jsonDrivenTestFactory.CreateTest(@object, methodName);
                test.Arrange(operation);
                tests.Add(test);
            }
            return tests;
        }
    }
}