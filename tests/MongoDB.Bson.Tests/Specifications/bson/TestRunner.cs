/* Copyright 2016 MongoDB Inc.
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Specifications.bson
{
    public class TestRunner
    {
        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(TestType testType, BsonDocument definition)
        {
            switch (testType)
            {
                case TestType.Valid:
                    RunValid(definition);
                    break;
                case TestType.ParseError:
                    RunParseError(definition);
                    break;
            }
        }

        private void RunValid(BsonDocument definition)
        {
            // see the pseudo code in the specification

            var B = BsonUtils.ParseHexString(((string)definition["bson"]).ToLowerInvariant());
            var E = ((string)definition["extjson"]).Replace(" ", "");

            byte[] cB;
            if (definition.Contains("canonical_bson"))
            {
                cB = BsonUtils.ParseHexString(((string)definition["canonical_bson"]).ToLowerInvariant());
            }
            else
            {
                cB = B;
            }

            string cE;
            if (definition.Contains("canonical_extjson"))
            {
                cE = ((string)definition["canonical_extjson"]).Replace(" ", "");
            }
            else
            {
                cE = E;
            }

            EncodeBson(DecodeBson(B)).Should().Equal(cB, "B -> cB");

            if (B != cB)
            {
                EncodeBson(DecodeBson(cB)).Should().Equal(cB, "cB -> cB");
            }

            if (definition.Contains("extjson"))
            {
                EncodeExtjson(DecodeBson(B)).Should().Be(cE, "B -> cE");
                EncodeExtjson(DecodeExtjson(E)).Should().Be(cE, "E -> cE");

                if (B != cB)
                {
                    EncodeExtjson(DecodeBson(cB)).Should().Be(cE, "cB -> cE");
                }

                if (E != cE)
                {
                    EncodeExtjson(DecodeExtjson(cE)).Should().Be(cE, "cE -> cE");
                }

                if (!definition.GetValue("lossy", false).ToBoolean())
                {
                    EncodeBson(DecodeExtjson(E)).Should().Equal(cB, "E -> cB");

                    if (E != cE)
                    {
                        EncodeBson(DecodeExtjson(cE)).Should().Equal(cB, "cE -> cB");
                    }
                }
            }
        }

        private BsonDocument DecodeBson(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BsonBinaryReader(stream))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                return BsonDocumentSerializer.Instance.Deserialize(context);
            }
        }

        private BsonDocument DecodeExtjson(string extjson)
        {
            return BsonDocument.Parse(extjson);
        }

        private byte[] EncodeBson(BsonDocument document)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BsonBinaryWriter(stream))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                BsonDocumentSerializer.Instance.Serialize(context, document);
                return stream.ToArray();
            }
        }

        private string EncodeExtjson(BsonDocument document)
        {
            var json = document.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Strict });
            return json.Replace(" ", "");
        }

        private void RunParseError(BsonDocument definition)
        {
            var subject = (string)definition["string"];
            Decimal128 result;
            if (Decimal128.TryParse(subject, out result))
            {
                Assert.True(false, $"{subject} should have resulted in a parse failure.");
            }
        }

        public enum TestType
        {
            Valid,
            ParseError
        }

        private class TestCaseFactory : IEnumerable<object[]>
        {
            public  IEnumerator<object[]> GetEnumerator()
            {
#if NETSTANDARD1_5 || NETSTANDARD1_6
                const string prefix = "MongoDB.Bson.Tests.Dotnet.Specifications.bson.tests.";
#else
                const string prefix = "MongoDB.Bson.Tests.Specifications.bson.tests.";
#endif
                var executingAssembly = typeof(TestCaseFactory).GetTypeInfo().Assembly;
                var enumerable = executingAssembly
                    .GetManifestResourceNames()
                    .Where(path => path.StartsWith(prefix) && path.EndsWith(".json"))
                    .SelectMany(path =>
                    {
                        var definition = ReadDefinition(path);
                        var fullName = path.Remove(0, prefix.Length);

                        var tests = Enumerable.Empty<object[]>();

                        if (definition.Contains("valid"))
                        {
                            tests = tests.Concat(GetTestCasesHelper(
                                TestType.Valid,
                                (string)definition["description"],
                                definition["valid"].AsBsonArray.Cast<BsonDocument>()));
                        }
                        if (definition.Contains("parseErrors"))
                        {
                            tests = tests.Concat(GetTestCasesHelper(
                            TestType.ParseError,
                            (string)definition["description"],
                            definition["parseErrors"].AsBsonArray.Cast<BsonDocument>()));
                        }

                        return tests;
                    });
                return enumerable.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private static IEnumerable<object[]> GetTestCasesHelper(TestType type, string description, IEnumerable<BsonDocument> documents)
            {
                var nameList = new Dictionary<string, int>();
                foreach (BsonDocument document in documents)
                {
                    var data = new object[] { type, document };

                    //data.SetCategory("Specifications");
                    //data.SetCategory("bson");

                    //var name = GetTestName(description, document);
                    //int i = 0;
                    //if (nameList.TryGetValue(name, out i))
                    //{
                    //    nameList[name] = i + 1;
                    //    name += " #" + i;
                    //}
                    //else
                    //{
                    //    nameList[name] = 1;
                    //}
                    //data.SetName(name);

                    yield return data;
                }
            }

            private static string GetTestName(string description, BsonDocument definition)
            {
                var name = description;
                if (definition.Contains("description"))
                {
                    name += " - " + (string)definition["description"];
                }

                return name;
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