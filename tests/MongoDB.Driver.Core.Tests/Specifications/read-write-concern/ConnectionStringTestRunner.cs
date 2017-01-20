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
    public class ConnectionStringTestRunner
    {
        [Theory]
        [ClassData(typeof(TestCaseFactory))]
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

        private class TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
#if NET45
                const string prefix = "MongoDB.Driver.Specifications.read_write_concern.tests.connection_string.";
#else
                const string prefix = "MongoDB.Driver.Core.Tests.Dotnet.Specifications.read_write_concern.tests.connection_string.";
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
                            //data.SetCategory("ConnectionString");
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
