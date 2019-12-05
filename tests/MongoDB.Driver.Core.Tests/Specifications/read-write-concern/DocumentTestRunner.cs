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
using Xunit;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Specifications.read_write_concern.tests
{
    public class DocumentTestRunner
    {
        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            var definition = testCase.Test;

            JsonDrivenHelper.EnsureAllFieldsAreValid(definition, "description", "valid", "readConcern", "readConcernDocument", "isServerDefault", "writeConcern", "writeConcernDocument", "isAcknowledged");

            BsonValue readConcernValue;
            if (definition.TryGetValue("readConcern", out readConcernValue))
            {
                ValidateReadConcern(definition);
            }

            BsonValue writeConcernValue;
            if (definition.TryGetValue("writeConcern", out writeConcernValue))
            {
                ValidateWriteConcern(definition);
            }
        }

        private void ValidateReadConcern(BsonDocument definition)
        {
            Exception parseException = null;
            ReadConcern readConcern = null;
            try
            {
                readConcern = ReadConcern.FromBsonDocument((BsonDocument)definition["readConcern"]);
            }
            catch (Exception ex)
            {
                parseException = ex;
            }

            if (parseException == null)
            {
                if (!(bool)definition["valid"])
                {
                    throw new AssertionException($"Should be invalid: {definition["readConcern"]}.");
                }

                var expectedDocument = (BsonDocument)definition["readConcernDocument"];
                var document = readConcern.ToBsonDocument();
                document.Should().Be(expectedDocument);

                readConcern.IsServerDefault.Should().Be((bool)definition["isServerDefault"]);
            }
            else
            {
                if ((bool)definition["valid"])
                {
                    throw new AssertionException($"Should be valid: {definition["readConcern"]}.");
                }
            }
        }

        private void ValidateWriteConcern(BsonDocument definition)
        {
            Exception parseException = null;
            WriteConcern writeConcern = null;
            try
            {
                writeConcern = WriteConcern.FromBsonDocument(MassageWriteConcernDocument((BsonDocument)definition["writeConcern"]));
            }
            catch (Exception ex)
            {
                parseException = ex;
            }

            if (parseException == null)
            {
                if (!(bool)definition["valid"])
                {
                    throw new AssertionException($"Should be invalid: {definition["writeConcern"]}.");
                }

                var expectedDocument = (BsonDocument)definition["writeConcernDocument"];
                var document = writeConcern.ToBsonDocument();
                document.Should().Be(expectedDocument);

                writeConcern.IsServerDefault.Should().Be((bool)definition["isServerDefault"]);
                writeConcern.IsAcknowledged.Should().Be((bool)definition["isAcknowledged"]);
            }
            else
            {
                if ((bool)definition["valid"])
                {
                    throw new AssertionException($"Should be valid: {definition["writeConcern"]}.");
                }
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
            protected override string PathPrefix => "MongoDB.Driver.Core.Tests.Specifications.read_write_concern.tests.document.";
        }
    }
}
