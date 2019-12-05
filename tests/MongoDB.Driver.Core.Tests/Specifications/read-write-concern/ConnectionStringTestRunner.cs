/* Copyright 2015-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;
using Xunit;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Specifications.read_write_concern.tests
{
    public class ConnectionStringTestRunner
    {
        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            var definition = testCase.Test;

            JsonDrivenHelper.EnsureAllFieldsAreValid(definition, "description", "uri", "valid", "warning", "readConcern", "writeConcern");

            ConnectionString connectionString = null;
            Exception parseException = null;
            try
            {
                connectionString = new ConnectionString((string)definition["uri"]);
            }
            catch (Exception ex)
            {
                parseException = ex;
            }

            if (parseException == null)
            {
                AssertValid(connectionString, definition);
            }
            else
            {
                AssertInvalid(parseException, definition);
            }
        }

        private void AssertValid(ConnectionString connectionString, BsonDocument definition)
        {
            if (!definition["valid"].ToBoolean())
            {
                throw new AssertionException($"The connection string '{definition["uri"]}' should be invalid.");
            }

            BsonValue readConcernValue;
            if (definition.TryGetValue("readConcern", out readConcernValue))
            {
                var readConcern = ReadConcern.FromBsonDocument((BsonDocument)readConcernValue);

                connectionString.ReadConcernLevel.Should().Be(readConcern.Level);
            }

            BsonValue writeConcernValue;
            if (definition.TryGetValue("writeConcern", out writeConcernValue))
            {
                var writeConcern =
                    WriteConcern.FromBsonDocument(MassageWriteConcernDocument((BsonDocument) writeConcernValue));
                connectionString.W.Should().Be(writeConcern.W);
                connectionString.WTimeout.Should().Be(writeConcern.WTimeout);
                connectionString.Journal.Should().Be(writeConcern.Journal);
                connectionString.FSync.Should().Be(writeConcern.FSync);
            }
        }

        private void AssertInvalid(Exception ex, BsonDocument definition)
        {
            // we will assume warnings are allowed to be errors...
            if (definition["valid"].ToBoolean() && !definition["warning"].ToBoolean())
            {
                throw new AssertionException($"The connection string '{definition["uri"]}' should be valid.", ex);
            }
        }

        private BsonDocument MassageWriteConcernDocument(BsonDocument writeConcern)
        {
            if (writeConcern.Contains("wtimeoutMS"))
            {
                writeConcern["wtimeout"] = writeConcern["wtimeoutMS"];
                writeConcern.Remove("wtimeoutMS");
            }

            if (writeConcern.Contains("journal"))
            {
                writeConcern["j"] = writeConcern["journal"];
                writeConcern.Remove("journal");
            }

            return writeConcern;
        }

        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            protected override string PathPrefix => "MongoDB.Driver.Core.Tests.Specifications.read_write_concern.tests.connection_string.";
        }
    }
}
