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
    public class ConnectionStringTestRunner
    {
        [TestCaseSource(typeof(TestCaseFactory), "GetTestCases")]
        public void RunTestDefinition(BsonDocument definition)
        {
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
                Assert.Fail($"The connection string '{definition["uri"]}' should be invalid.");
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
                var writeConcern = WriteConcern.FromBsonDocument(MassageWriteConcernDocument((BsonDocument)writeConcernValue));

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

        private static class TestCaseFactory
        {
            public static IEnumerable<ITestCaseData> GetTestCases()
            {
                const string prefix = "MongoDB.Driver.Specifications.read_write_concern.tests.connection_string.";
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
                            data.Categories.Add("ConnectionString");
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
