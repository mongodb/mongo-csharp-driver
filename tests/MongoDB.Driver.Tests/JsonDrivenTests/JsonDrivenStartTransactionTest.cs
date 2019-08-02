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
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenStartTransactionTest : JsonDrivenSessionTest
    {
        // private fields
        private TransactionOptions _options = null;

        // public constructors
        public JsonDrivenStartTransactionTest(Dictionary<string, object> objectMap)
            : base(objectMap)
        {
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
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            _session.StartTransaction(_options);
        }

        protected override Task CallMethodAsync(CancellationToken cancellationToken)
        {
            _session.StartTransaction(_options); // there is no async version, just call the sync version
            return Task.FromResult(true);
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "options":
                    _options = ParseOptions(value.AsBsonDocument);
                    return;
            }

            base.SetArgument(name, value);
        }

        // private methods
        private TransactionOptions ParseOptions(BsonDocument document)
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
    }
}
