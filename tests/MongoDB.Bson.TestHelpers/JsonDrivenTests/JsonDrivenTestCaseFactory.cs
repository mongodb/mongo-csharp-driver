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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit.Abstractions;

namespace MongoDB.Bson.TestHelpers.JsonDrivenTests
{
    public abstract class JsonDrivenTestCaseFactory<TTestCase> : IEnumerable<object[]>
        where TTestCase : IXunitSerializable
    {
        // protected properties
        protected virtual Assembly Assembly => this.GetType().GetTypeInfo().Assembly;

        protected abstract string PathPrefix { get; }

        // public methods
        public IEnumerator<object[]> GetEnumerator()
        {
            return
                ReadJsonDocuments()
                .SelectMany(document => CreateTestCases(document))
                .Select(testCase => new object[] { testCase })
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // protected methods
        protected abstract IEnumerable<TTestCase> CreateTestCases(BsonDocument document);

        protected virtual BsonDocument ReadJsonDocument(string path)
        {
            using (var stream = Assembly.GetManifestResourceStream(path))
            using (var reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                var document = BsonDocument.Parse(json);
                document.InsertAt(0, new BsonElement("_path", path));
                return document;
            }
        }

        protected virtual IEnumerable<BsonDocument> ReadJsonDocuments()
        {
            return
                Assembly.GetManifestResourceNames()
                .Where(path => ShouldReadJsonDocument(path))
                .Select(path => ReadJsonDocument(path));
        }

        protected virtual bool ShouldReadJsonDocument(string path)
        {
            return path.StartsWith(PathPrefix) && path.EndsWith(".json");
        }
    }

    public abstract class JsonDrivenTestCaseFactory : JsonDrivenTestCaseFactory<JsonDrivenTestCase>
    {
        // protected methods
        protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
        {
            var shared = document;

            if (shared.Contains("tests"))
            {
                var tests = shared["tests"].AsBsonArray.Select(item => item.AsBsonDocument).ToList();

                for (var i = 0; i < tests.Count; i++)
                {
                    var test = tests[i];
                    var name = GetTestCaseName(shared, test, i);
                    yield return new JsonDrivenTestCase(name, shared, test);
                }

                yield break;
            }

            throw new FormatException("Could not find any test cases.");
        }

        protected virtual string GetTestCaseName(BsonDocument shared, BsonDocument test, int index)
        {
            var path = shared["_path"].AsString;
            var name = GetTestName(test, index);
            return $"{path}:{name}";
        }

        protected virtual string GetTestName(BsonDocument test, int index)
        {
            if (test.Contains("description"))
            {
                return test["description"].AsString;
            }
            else
            {
                return $"[{index}]";
            }
        }
    }
}
