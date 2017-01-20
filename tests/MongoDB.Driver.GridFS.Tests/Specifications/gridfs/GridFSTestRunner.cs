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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests;
using Xunit;

namespace MongoDB.Driver.GridFS.Tests.Specifications.gridfs
{
    public class GridFSTestRunner
    {
        [SkippableTheory]
        [ClassData(typeof(TestCaseSource))]
        [Trait("Category", "Specifications_gridfs")]
        public void RunTest(BsonDocument data, BsonDocument testDefinition)
        {
            var test = GridFSTestFactory.CreateTest(data, testDefinition);

            string reason;
            if (!test.CanRun(out reason))
            {
                throw new SkipTestException(reason);
            }

            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var bucket = new GridFSBucket(database);

            test.Run(bucket, async: false);
            test.Run(bucket, async: true);
        }

        public class TestCaseSource : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
#if NET45
                const string prefix = "MongoDB.Driver.GridFS.Tests.Specifications.gridfs.tests.";
#else
                const string prefix = "MongoDB.Driver.GridFS.Tests.Dotnet.Specifications.gridfs.tests.";
#endif
                var testCases = typeof(TestCaseSource).GetTypeInfo().Assembly
                    .GetManifestResourceNames()
                    .Where(path => path.StartsWith(prefix) && path.EndsWith(".json"))
                    .SelectMany(path =>
                    {
                        var testFileContents = ReadTestFile(path);
                        var data = (BsonDocument)testFileContents.GetValue("data", null);
                        var testDefinitions = testFileContents["tests"].AsBsonArray;
                        return testDefinitions.Select(testDefinition =>
                        {
                            //return new TestCaseData(data, testDefinition)
                            //    .SetCategory("Specifications_gridfs")
                            //    .SetName(testDefinition["description"].AsString);
                            return new object[] { data, testDefinition };
                        });
                    })
                    .ToList();
                return testCases.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private BsonValue PreprocessHex(BsonValue value)
            {
                var array = value as BsonArray;
                if (array != null)
                {
                    for (var i = 0; i < array.Count; i++)
                    {
                        array[i] = PreprocessHex(array[i]);
                    }
                    return array;
                }

                var document = value as BsonDocument;
                if (document != null)
                {
                    if (document.ElementCount == 1 && document.GetElement(0).Name == "$hex" && document[0].IsString)
                    {
                        var hex = document[0].AsString;
                        var bytes = BsonUtils.ParseHexString(hex);
                        return new BsonBinaryData(bytes);
                    }

                    for (var i = 0; i < document.ElementCount; i++)
                    {
                        document[i] = PreprocessHex(document[i]);
                    }
                    return document;
                }

                return value;
            }

            private BsonDocument ReadTestFile(string path)
            {
                using (var stream = typeof(TestCaseSource).GetTypeInfo().Assembly.GetManifestResourceStream(path))
                using (var streamReader = new StreamReader(stream))
                {
                    var contents = streamReader.ReadToEnd();
                    var document = BsonDocument.Parse(contents);
                    return (BsonDocument)PreprocessHex(document);
                }
            }
        }
    }
}
