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
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenStartTransactionTest : JsonDrivenClientTest
    {
        // private fields
        private TransactionOptions _options = null;

        // public constructors
        public JsonDrivenStartTransactionTest(IMongoClient client, Dictionary<string, IClientSessionHandle> sessionMap)
            : base(client, sessionMap)
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "arguments", "result");
            base.Arrange(document);
        }

        // protected methods
        protected override void CallMethod(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            session.StartTransaction(_options);
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "options":
                    SetOptions(value.AsBsonDocument);
                    return;
            }

            base.SetArgument(name, value);
        }

        // private methods
        private void SetOptions(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "readConcern", "readPreference", "writeConcern");

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

            _options = options;
        }
    }
}
