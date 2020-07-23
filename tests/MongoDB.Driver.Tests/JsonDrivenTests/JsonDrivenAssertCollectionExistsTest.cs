/* Copyright 2020-present MongoDB Inc.
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
    public class JsonDrivenAssertCollectionExistsTest : JsonDrivenTestRunnerTest
    {
        // private fields
        private string _collectionName;
        private string _databaseName;

        // public constructors
        public JsonDrivenAssertCollectionExistsTest(IJsonDrivenTestRunner testRunner, Dictionary<string, object> objectMap)
            : base(testRunner, objectMap)
        {
        }

        // public methods
        public override void Act(CancellationToken cancellationToken)
        {
            // do nothing
        }

        public override Task ActAsync(CancellationToken cancellationToken)
        {
            // do nothing
            return Task.FromResult(true);
        }

        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "arguments");
            base.Arrange(document);
        }

        public override void Assert()
        {
            var client = DriverTestConfiguration.Client;
            var collectionNames = client.GetDatabase(_databaseName).ListCollectionNames().ToList();
            collectionNames.Should().Contain(_collectionName);
        }

        // protected methods
        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "collection":
                    _collectionName = value.AsString;
                    return;
                case "database":
                    _databaseName = value.AsString;
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
