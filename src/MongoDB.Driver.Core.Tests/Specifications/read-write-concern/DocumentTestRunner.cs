/* Copyright 2015 MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using NUnit.Framework;

namespace MongoDB.Driver.Specifications.read_write_concern.tests
{
    [TestFixture]
    public class DocumentTestRunner
    {
        [TestCaseSource(typeof(TestCaseFactory), "GetTestCases")]
        public void RunTestDefinition(BsonDocument definition)
        {
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

        private static class TestCaseFactory
        {
            public static IEnumerable<ITestCaseData> GetTestCases()
            {
                const string prefix = "MongoDB.Driver.Specifications.read_write_concern.tests.document.";
                return Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceNames()
                    .Where(path => path.StartsWith(prefix) && path.EndsWith(".json"))
                    .SelectMany(path =>
                    {
                        var definition = ReadDefinition(path);
                        var tests = (BsonArray)definition["tests"];
                        var fullName = path.Remove(0, prefix.Length);
                        var list = new List<TestCaseData>();
                        foreach (BsonDocument test in tests)
                        {
                            var data = new TestCaseData(test);
                            data.Categories.Add("Specifications");
                            if (test.Contains("readConcern"))
                            {
                                data.Categories.Add("ReadConcern");
                            }
                            else
                            {
                                data.Categories.Add("WriteConcern");
                            }
                            var testName = fullName.Remove(fullName.Length - 5) + ": " + test["description"];
                            list.Add(data.SetName(testName));
                        }
                        return list;
                    });
            }

            private static BsonDocument ReadDefinition(string path)
            {
                using (var definitionStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
                using (var definitionStringReader = new StreamReader(definitionStream))
                {
                    var definitionString = definitionStringReader.ReadToEnd();
                    return BsonDocument.Parse(definitionString);
                }
            }
        }
    }
}
