/* Copyright 2010-present MongoDB Inc.
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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonArraySerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonArray value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonArray V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var obj = new TestClass(new BsonArray());
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "[]").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNotEmpty()
        {
            var obj = new TestClass(new BsonArray { 1, 2 });
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "[1, 2]").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonBinaryGuidSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonBinaryData value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonBinaryData V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void TestEmpty(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode)
        {
            mode.Set();

#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2 && BsonDefaults.GuidRepresentation != GuidRepresentation.Unspecified)
            {
                var obj = new TestClass(Guid.Empty);

                string expectedGuidJson;
                var guidRepresentation = BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2 ? BsonDefaults.GuidRepresentation : GuidRepresentation.Unspecified;
                switch (guidRepresentation)
                {
                    case GuidRepresentation.CSharpLegacy: expectedGuidJson = "CSUUID('00000000-0000-0000-0000-000000000000')"; break;
                    case GuidRepresentation.JavaLegacy: expectedGuidJson = "JUUID('00000000-0000-0000-0000-000000000000')"; break;
                    case GuidRepresentation.PythonLegacy: expectedGuidJson = "PYUUID('00000000-0000-0000-0000-000000000000')"; break;
                    case GuidRepresentation.Standard: expectedGuidJson = "UUID('00000000-0000-0000-0000-000000000000')"; break;
                    default: throw new Exception("Unexpected GuidRepresentation.");
                }

                var json = obj.ToJson(new JsonWriterSettings());
                var expected = "{ 'B' : #, 'V' : # }".Replace("#", expectedGuidJson).Replace("'", "\"");
                Assert.Equal(expected, json);

                var bson = obj.ToBson();
                var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
                Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
            }
            else
            {
                var exception = Record.Exception(() => new TestClass(Guid.Empty));
                exception.Should().BeOfType<InvalidOperationException>();
            }
#pragma warning disable 618
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void TestNew(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode)
        {
            mode.Set();

#pragma warning disable 618
            var guid = Guid.NewGuid();
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2 && BsonDefaults.GuidRepresentation != GuidRepresentation.Unspecified)
            {
                var obj = new TestClass(guid);

                string expectedGuidJson;
                var guidRepresentation = BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2 ? BsonDefaults.GuidRepresentation : GuidRepresentation.Unspecified;
                switch (guidRepresentation)
                {
                    case GuidRepresentation.CSharpLegacy: expectedGuidJson = $"CSUUID('{guid.ToString()}')"; break;
                    case GuidRepresentation.JavaLegacy: expectedGuidJson = $"JUUID('{guid.ToString()}')"; break;
                    case GuidRepresentation.PythonLegacy: expectedGuidJson = $"PYUUID('{guid.ToString()}')"; break;
                    case GuidRepresentation.Standard: expectedGuidJson = $"UUID('{guid.ToString()}')"; break;
                    default: throw new Exception("Unexpected GuidRepresentation.");
                }

                var json = obj.ToJson(new JsonWriterSettings());
                var expected = "{ 'B' : #, 'V' : # }".Replace("#", expectedGuidJson).Replace("'", "\"");
                Assert.Equal(expected, json);

                var bson = obj.ToBson();
                var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
                Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
            }
            else
            {
                var exception = Record.Exception(() => new TestClass(guid));
                exception.Should().BeOfType<InvalidOperationException>();
            }
#pragma warning disable 618
        }
    }

    public class BsonBooleanSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonBoolean value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonBoolean V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestFalse()
        {
            var obj = new TestClass(false);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "false").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestTrue()
        {
            var obj = new TestClass(true);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "true").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonDateTimeSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonDateTime value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonDateTime V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinBson()
        {
            var obj = new TestClass(new BsonDateTime(long.MinValue));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "new Date(-9223372036854775808)").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinLocal()
        {
            var obj = new TestClass(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "ISODate('0001-01-01T00:00:00Z')").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinUnspecified()
        {
            var obj = new TestClass(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "ISODate('0001-01-01T00:00:00Z')").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinUtc()
        {
            var obj = new TestClass(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "ISODate('0001-01-01T00:00:00Z')").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMaxBson()
        {
            var obj = new TestClass(new BsonDateTime(long.MaxValue));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "new Date(9223372036854775807)").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMaxLocal()
        {
            var obj = new TestClass(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "ISODate('9999-12-31T23:59:59.999Z')").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMaxUnspecified()
        {
            var obj = new TestClass(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Unspecified));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "ISODate('9999-12-31T23:59:59.999Z')").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMaxUtc()
        {
            var obj = new TestClass(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "ISODate('9999-12-31T23:59:59.999Z')").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestLocal()
        {
            var obj = new TestClass(new DateTime(2010, 10, 08, 13, 30, 0, DateTimeKind.Local));
            var isoDate = string.Format("ISODate(\"{0}\")", obj.V.ToUniversalTime().ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"));
            long milliseconds = (long)(obj.V.ToUniversalTime() - BsonConstants.UnixEpoch).TotalMilliseconds;
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", isoDate).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestUnspecified()
        {
            var obj = new TestClass(new DateTime(2010, 10, 08, 13, 30, 0, DateTimeKind.Unspecified));
            var isoDate = string.Format("ISODate(\"{0}\")", obj.V.ToUniversalTime().ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", isoDate).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestUtc()
        {
            var obj = new TestClass(new DateTime(2010, 10, 08, 13, 30, 0, DateTimeKind.Utc));
            var isoDate = string.Format("ISODate(\"{0}\")", obj.V.ToUniversalTime().ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", isoDate).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonDocumentSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonDocument value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonDocument V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ \'_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var obj = new TestClass(new BsonDocument());
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNotEmpty()
        {
            var obj = new TestClass(
                new BsonDocument
                {
                    { "A", 1 },
                    { "B", 2 }
                });
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ 'A' : 1, 'B' : 2 }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void GetDocumentId_should_return_expected_result_when_id_is_missing()
        {
            var subject = new BsonDocumentSerializer();
            var document = new BsonDocument();

            var result = subject.GetDocumentId(document, out var id, out var idNominalType, out var idGenerator);

            result.Should().BeTrue();
            id.Should().BeNull();
            idNominalType.Should().Be(typeof(BsonValue));
            idGenerator.Should().Be(BsonObjectIdGenerator.Instance);
        }

        [Fact]
        public void GetDocumentId_should_return_expected_result_when_id_is_ObjectId()
        {
            var subject = new BsonDocumentSerializer();
            var document = new BsonDocument("_id", ObjectId.GenerateNewId());

            var result = subject.GetDocumentId(document, out var id, out var idNominalType, out var idGenerator);

            result.Should().BeTrue();
            id.Should().Be(document["_id"]);
            idNominalType.Should().Be(typeof(BsonValue));
            idGenerator.Should().Be(BsonObjectIdGenerator.Instance);
        }

        [Fact]
        public void GetDocumentId_should_return_expected_result_when_id_is_int32()
        {
            var subject = new BsonDocumentSerializer();
            var document = new BsonDocument("_id", 1);

            var result = subject.GetDocumentId(document, out var id, out var idNominalType, out var idGenerator);

            result.Should().BeTrue();
            id.Should().Be(document["_id"]);
            idNominalType.Should().Be(typeof(BsonValue));
            idGenerator.Should().BeNull();
        }

        public static IEnumerable<object[]> GetDocumentId_should_return_expected_result_when_id_is_binary_data_guid_MemberData()
        {
            var data = new TheoryData<GuidRepresentationMode, GuidRepresentation, GuidRepresentation>();

            foreach (var defaultGuidRepresentationMode in EnumHelper.GetValues<GuidRepresentationMode>())
            {
                foreach (var defaultGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
                {
                    if (defaultGuidRepresentationMode == GuidRepresentationMode.V3 && defaultGuidRepresentation != GuidRepresentation.Unspecified)
                    {
                        continue;
                    }

                    foreach (var idGuidRepresentation in EnumHelper.GetValues<GuidRepresentation>())
                    {
                        if (idGuidRepresentation == GuidRepresentation.Unspecified)
                        {
                            continue;
                        }

                        data.Add(defaultGuidRepresentationMode, defaultGuidRepresentation, idGuidRepresentation);
                    }
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(GetDocumentId_should_return_expected_result_when_id_is_binary_data_guid_MemberData))]
        [ResetGuidModeAfterTest]
        public void GetDocumentId_should_return_expected_result_when_id_is_binary_data_guid(
            GuidRepresentationMode defaultGuidRepresentationMode,
            GuidRepresentation defaultGuidRepresentation,
            GuidRepresentation idGuidRepresentation)
        {
            GuidMode.Set(defaultGuidRepresentationMode, defaultGuidRepresentation);

            var subject = new BsonDocumentSerializer();
            var guid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
            var document = new BsonDocument("_id", new BsonBinaryData(guid, idGuidRepresentation));

            var result = subject.GetDocumentId(document, out var id, out var idNominalType, out var idGenerator);

            result.Should().BeTrue();
            id.Should().Be(document["_id"]);
            idNominalType.Should().Be(typeof(BsonValue));
            if (defaultGuidRepresentationMode == GuidRepresentationMode.V2 || idGuidRepresentation == GuidRepresentation.Standard)
            {
                var guidGenerator = idGenerator.Should().BeOfType<BsonBinaryDataGuidGenerator>().Subject;
                guidGenerator.GuidRepresentation.Should().Be(idGuidRepresentation);
            }
            else
            {
                idGenerator.Should().BeNull();
            }
        }
    }

    public class BsonDocumentWrapperSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonDocumentWrapper value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonDocumentWrapper V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson); // can only be deserialized because V is C# null
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var obj = new TestClass(new BsonDocumentWrapper(new BsonDocument()));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            try
            {
                BsonSerializer.Deserialize<TestClass>(bson);
                throw new AssertionException("Expected an exception to be thrown.");
            }
            catch (Exception ex)
            {
                var expectedMessage = "An error occurred while deserializing the V property of class MongoDB.Bson.Tests.Serialization.BsonDocumentWrapperSerializerTests+TestClass";
                Assert.IsType<FormatException>(ex);
                Assert.IsType<NotSupportedException>(ex.InnerException);
                Assert.Equal(expectedMessage, ex.Message.Substring(0, ex.Message.IndexOf(':')));
            }
        }

        [Fact]
        public void TestNotEmpty()
        {
            var obj = new TestClass(
                new BsonDocumentWrapper(
                    new BsonDocument
                    {
                        { "A", 1 },
                        { "B", 2 }
                    }));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ 'A' : 1, 'B' : 2 }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            try
            {
                BsonSerializer.Deserialize<TestClass>(bson);
                throw new AssertionException("Expected an exception to be thrown.");
            }
            catch (Exception ex)
            {
                var expectedMessage = "An error occurred while deserializing the V property of class MongoDB.Bson.Tests.Serialization.BsonDocumentWrapperSerializerTests+TestClass";
                Assert.IsType<FormatException>(ex);
                Assert.IsType<NotSupportedException>(ex.InnerException);
                Assert.Equal(expectedMessage, ex.Message.Substring(0, ex.Message.IndexOf(':')));
            }
        }
    }

    public class BsonDoubleSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonDouble value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonDouble V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMin()
        {
            var obj = new TestClass(double.MinValue);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "-1.7976931348623157E+308").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinusOne()
        {
            var obj = new TestClass(-1.0);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "-1.0").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestZero()
        {
            var obj = new TestClass(0.0);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "0.0").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOne()
        {
            var obj = new TestClass(1.0);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "1.0").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMax()
        {
            var obj = new TestClass(double.MaxValue);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "1.7976931348623157E+308").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNaN()
        {
            var obj = new TestClass(double.NaN);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "NaN").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNegativeInfinity()
        {
            var obj = new TestClass(double.NegativeInfinity);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "-Infinity").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestPositiveInfinity()
        {
            var obj = new TestClass(double.PositiveInfinity);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "Infinity").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonInt32SerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonInt32 value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonInt32 V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMin()
        {
            var obj = new TestClass(int.MinValue);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", int.MinValue.ToString()).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinusOne()
        {
            var obj = new TestClass(-1);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "-1").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestZero()
        {
            var obj = new TestClass(0);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "0").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOne()
        {
            var obj = new TestClass(1);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "1").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMax()
        {
            var obj = new TestClass(int.MaxValue);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", int.MaxValue.ToString()).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonInt64SerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonInt64 value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonInt64 V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMin()
        {
            var obj = new TestClass(long.MinValue);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "NumberLong('-9223372036854775808')").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinusOne()
        {
            var obj = new TestClass(-1);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "NumberLong(-1)").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestZero()
        {
            var obj = new TestClass(0);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "NumberLong(0)").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOne()
        {
            var obj = new TestClass(1);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "NumberLong(1)").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMax()
        {
            var obj = new TestClass(long.MaxValue);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "NumberLong('9223372036854775807')").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonJavaScriptSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonJavaScript value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonJavaScript V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNotNull()
        {
            var obj = new TestClass("this.age === 21");
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$code' : 'this.age === 21' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonJavaScriptWithScopeSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonJavaScriptWithScope value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonJavaScriptWithScope V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNotNull()
        {
            var scope = new BsonDocument("x", 21);
            var obj = new TestClass(new BsonJavaScriptWithScope("this.age === 21", scope));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$code' : 'this.age === 21', '$scope' : { 'x' : 21 } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonMaxKeySerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonMaxKey value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonMaxKey V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestValue()
        {
            var obj = new TestClass(BsonMaxKey.Value);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "MaxKey").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Same(obj.V, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonMinKeySerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonMinKey value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonMinKey V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestValue()
        {
            var obj = new TestClass(BsonMinKey.Value);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "MinKey").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Same(obj.V, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonNullSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonNull value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonNull V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));

            // test that we can still deserialize the legacy representation for a BsonNull value of C# null
            var legacy = expected.Replace("_csharpnull", "$csharpnull");
            rehydrated = BsonSerializer.Deserialize<TestClass>(legacy);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
        }

        [Fact]
        public void TestValue()
        {
            var obj = new TestClass(BsonNull.Value);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "null").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Same(obj.V, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonObjectIdSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonObjectId value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonObjectId V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNotNull()
        {
#pragma warning disable 618
            var obj = new TestClass(new ObjectId(1, 2, 3, 4));
#pragma warning restore 618
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "ObjectId('000000010000020003000004')").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonRegularExpressionSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonRegularExpression value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonRegularExpression V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestWithoutOptions()
        {
            var obj = new TestClass(new BsonRegularExpression("abc"));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "/abc/").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestWithOptions()
        {
            var obj = new TestClass(new BsonRegularExpression("abc", "imxs"));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "/abc/imsx").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonStringObjectIdTests
    {
        public class C
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id;
            public int N;
        }

        [Fact]
        public void TestNull()
        {
            var obj = new C { Id = null, N = 1 };
            var json = obj.ToJson();
            var expected = "{ '_id' : null, 'N' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNotNull()
        {
            var id = ObjectId.Parse("123456789012345678901234");
            var obj = new C { Id = id.ToString(), N = 1 };
            var json = obj.ToJson();
            var expected = "{ '_id' : ObjectId('123456789012345678901234'), 'N' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonStringSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonString value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonString V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var obj = new TestClass("");
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "''").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestHelloWorld()
        {
            var obj = new TestClass("Hello World");
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "'Hello World'").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonSymbolSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonSymbol value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonSymbol V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var obj = new TestClass("");
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$symbol' : '' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Same(obj.V, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestHelloWorld()
        {
            var obj = new TestClass("Hello World");
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '$symbol' : 'Hello World' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Same(obj.V, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonTimestampSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonTimestamp value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonTimestamp V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMin()
        {
            var obj = new TestClass(new BsonTimestamp(long.MinValue));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "Timestamp(2147483648, 0)").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinusOne()
        {
            var obj = new TestClass(new BsonTimestamp(-1));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "Timestamp(4294967295, 4294967295)").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestZero()
        {
            var obj = new TestClass(new BsonTimestamp(0));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "Timestamp(0, 0)").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOne()
        {
            var obj = new TestClass(new BsonTimestamp(1));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "Timestamp(0, 1)").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOneTwo()
        {
            var obj = new TestClass(new BsonTimestamp(1, 2));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "Timestamp(1, 2)").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMax()
        {
            var obj = new TestClass(new BsonTimestamp(long.MaxValue));
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "Timestamp(2147483647, 4294967295)").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class BsonUndefinedSerializerTests
    {
        public class TestClass
        {
            public TestClass() { }

            public TestClass(BsonUndefined value)
            {
                this.B = value;
                this.V = value;
            }

            public BsonValue B { get; set; }
            public BsonUndefined V { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass(null);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "{ '_csharpnull' : true }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(null, rehydrated.B);
            Assert.Equal(null, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestValue()
        {
            var obj = new TestClass(BsonUndefined.Value);
            var json = obj.ToJson();
            var expected = "{ 'B' : #, 'V' : # }".Replace("#", "undefined").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Same(obj.V, rehydrated.V);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
