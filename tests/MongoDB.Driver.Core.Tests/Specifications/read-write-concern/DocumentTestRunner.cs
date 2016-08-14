/* Copyright 2015-2016 MongoDB Inc.
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
using Xunit;
using Xunit.Sdk;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using System.Collections;

namespace MongoDB.Driver.Specifications.read_write_concern.tests
{
    public class DocumentTestRunner
    {
        [Theory]
        [ClassData(typeof(TestCaseFactory))]
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

        private class TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
#if NET45
                const string prefix = "MongoDB.Driver.Specifications.read_write_concern.tests.document.";
#else
                const string prefix = "MongoDB.Driver.Core.Tests.Dotnet.Specifications.read_write_concern.tests.document.";
#endif
                var executingAssembly = typeof(TestCaseFactory).GetTypeInfo().Assembly;
                var enumerable = executingAssembly
                    .GetManifestResourceNames()
                    .Where(path => path.StartsWith(prefix) && path.EndsWith(".json"))
                    .SelectMany(path =>
                    {
                        var definition = ReadDefinition(path);
                        var tests = (BsonArray)definition["tests"];
                        var fullName = path.Remove(0, prefix.Length);
                        var list = new List<object[]>();
                        foreach (BsonDocument test in tests)
                        {
                            //var data = new TestCaseData(test);
                            //data.SetCategory("Specifications");
                            //if (test.Contains("readConcern"))
                            //{
                            //    data.SetCategory("ReadConcern");
                            //}
                            //else
                            //{
                            //    data.SetCategory("WriteConcern");
                            //}
                            //var testName = fullName.Remove(fullName.Length - 5) + ": " + test["description"];
                            //data = data.SetName(testName);
                            var data = new object[] { test };
                            list.Add(data);
                        }
                        return list;
                    });
                return enumerable.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private static BsonDocument ReadDefinition(string path)
            {
                var executingAssembly = typeof(TestCaseFactory).GetTypeInfo().Assembly;
                using (var definitionStream = executingAssembly.GetManifestResourceStream(path))
                using (var definitionStringReader = new StreamReader(definitionStream))
                {
                    var definitionString = definitionStringReader.ReadToEnd();
                    return BsonDocument.Parse(definitionString);
                }
            }
        }
    }
}
