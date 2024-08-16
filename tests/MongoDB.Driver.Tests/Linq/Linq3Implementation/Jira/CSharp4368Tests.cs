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
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4368Tests : Linq3IntegrationTest
    {
        private static readonly IBsonSerializer<Guid> __guidSerializerWithStandardRepresentation;
        private static readonly IBsonSerializer<Guid?> __nullableGuidSerializerWithStandardRepresentation;

        static CSharp4368Tests()
        {
            __guidSerializerWithStandardRepresentation = new GuidSerializer(GuidRepresentation.Standard);
            __nullableGuidSerializerWithStandardRepresentation = new NullableSerializer<Guid>(__guidSerializerWithStandardRepresentation);

            var guidClassMap = BsonClassMap.RegisterClassMap<Document<Guid>>(
                cm =>
                {
                    cm.MapMember(x => x.V).SetSerializer(__guidSerializerWithStandardRepresentation);
                });

            var nullableGuidClassMap = BsonClassMap.RegisterClassMap<Document<Guid?>>(
                cm =>
                {
                    cm.MapMember(x => x.V).SetSerializer(__nullableGuidSerializerWithStandardRepresentation);
                });
        }

        public class TestCase
        {
            public Type ValueType { get; set; }
            public string ValueAsJson { get; set; }
            public LambdaExpression Projection { get; set; }
        }

        public static TestCase CreateTestCase<TValue>(
            string valueAsJson,
            Expression<Func<Document<TValue>, BsonValue>> projection)
        {
            return new TestCase { ValueType = typeof(TValue), ValueAsJson = valueAsJson, Projection = projection };
        }

        public static TestCase[] __testCases = new TestCase[]
        {
            CreateTestCase<Anything>("{ X : 1 }", x => (BsonValue)(object)x.V),
            CreateTestCase<Anything>("null", x => (BsonValue)(object)x.V),
            CreateTestCase<byte[]>("BinData(0, 'AQID')'", x => (BsonValue)x.V),
            CreateTestCase<byte[]>("null", x => (BsonValue)x.V),
            CreateTestCase<bool>("true", x => (BsonValue)x.V),
            CreateTestCase<bool?>("true", x => (BsonValue)x.V),
            CreateTestCase<bool?>("null", x => (BsonValue)x.V),
            CreateTestCase<DateTime>("ISODate('2021-01-02T03:04:05.123')", x => (BsonValue)x.V),
            CreateTestCase<DateTime?>("ISODate('2021-01-02T03:04:05.123')", x => (BsonValue)x.V),
            CreateTestCase<DateTime?>("null", x => (BsonValue)x.V),
            CreateTestCase<decimal>("'1'", x => (BsonValue)x.V),
            CreateTestCase<decimal?>("'1'", x => (BsonValue)x.V),
            CreateTestCase<decimal?>("null", x => (BsonValue)x.V),
            CreateTestCase<Decimal128>("'1'", x => (BsonValue)x.V),
            CreateTestCase<Decimal128?>("'1'", x => (BsonValue)x.V),
            CreateTestCase<Decimal128?>("null", x => (BsonValue)x.V),
            CreateTestCase<double>("{ $numberDouble : '1.0' }", x => (BsonValue)x.V),
            CreateTestCase<double?>("{ $numberDouble : '1.0' }", x => (BsonValue)x.V),
            CreateTestCase<double?>("null", x => (BsonValue)x.V),
#pragma warning disable CS0618 // Type or member is obsolete
            CreateTestCase<Guid>("UUID('01020304-0506-0708-090a-0b0c0d0e0f10')", x => (BsonValue)x.V),
            CreateTestCase<Guid?>("UUID('01020304-0506-0708-090a-0b0c0d0e0f10')", x => (BsonValue)x.V),
            CreateTestCase<Guid?>("null", x => (BsonValue)x.V),
#pragma warning restore CS0618 // Type or member is obsolete
            CreateTestCase<Guid>("UUID('01020304-0506-0708-090a-0b0c0d0e0f10')", x => (BsonValue)(object)x.V),
            CreateTestCase<Guid?>("UUID('01020304-0506-0708-090a-0b0c0d0e0f10')", x => (BsonValue)(object)x.V),
            CreateTestCase<Guid?>("null", x => (BsonValue)(object)x.V),
            CreateTestCase<int>("1", x => (BsonValue)x.V),
            CreateTestCase<int?>("1", x => (BsonValue)x.V),
            CreateTestCase<int?>("null", x => (BsonValue)x.V),
            CreateTestCase<long>("{ $numberLong : '1' }", x => (BsonValue)x.V),
            CreateTestCase<long?>("{ $numberLong : '1' }", x => (BsonValue)x.V),
            CreateTestCase<long?>("null", x => (BsonValue)x.V),
            CreateTestCase<ObjectId>("ObjectId('0102030405060708090a0b0c')", x => (BsonValue)x.V),
            CreateTestCase<ObjectId?>("ObjectId('0102030405060708090a0b0c')", x => (BsonValue)x.V),
            CreateTestCase<ObjectId?>("null", x => (BsonValue)x.V),
            CreateTestCase<Regex>("/abc/i", x => (BsonValue)x.V),
            CreateTestCase<Regex>("null", x => (BsonValue)x.V),
            CreateTestCase<string>("'abc'", x => (BsonValue)x.V),
            CreateTestCase<string>("null", x => (BsonValue)x.V)
        };

        public static IEnumerable<object[]> Convert_to_BsonValue_from_TValue_should_work_MemberData()
        {
            for (var i = 0; i < __testCases.Length; i++)
            {
                var valueType = __testCases[i].ValueType;
                var valueAsJson = __testCases[i].ValueAsJson;
                var projectionAsString = __testCases[i].Projection.ToString();
                yield return new object[] { valueType, i, valueAsJson, projectionAsString  };
            }
        }

        [Theory]
        [MemberData(nameof(Convert_to_BsonValue_from_TValue_should_work_MemberData))]
        [ResetGuidModeAfterTest]
        public void Convert_to_BsonValue_from_TValue_should_work_invoker(Type valueType, int i, string valueAsJson, string projectionAsString)
        {
            GuidMode.Set(GuidRepresentationMode.V3);

            var testMethodInfo = this.GetType().GetMethod(nameof(Convert_to_BsonValue_from_TValue_should_work));
            var testMethod = testMethodInfo.MakeGenericMethod(valueType);
            testMethod.Invoke(this, new object[] { i, valueAsJson, projectionAsString });
        }

        public void Convert_to_BsonValue_from_TValue_should_work<TValue>(int i, string valueAsJson, string projectionAsString)
        {
            var serializer = typeof(TValue) switch
            {
                var type when type == typeof(Guid) => (IBsonSerializer<TValue>)__guidSerializerWithStandardRepresentation,
                var type when type == typeof(Guid?) => (IBsonSerializer<TValue>)__nullableGuidSerializerWithStandardRepresentation,
                _ => BsonSerializer.LookupSerializer<TValue>()
            };
            var value = Deserialize(serializer, valueAsJson);
            var projection = (Expression<Func<Document<TValue>, BsonValue>>)__testCases[i].Projection;
            var expectedResult = BsonSerializer.Deserialize<BsonValue>(valueAsJson);

            var collection = CreateCollection(value);

            var queryable = collection
                .AsQueryable()
                .Select(projection);

            var stages = Translate(collection, queryable, out var outputSerializer);
            AssertStages(
                stages,
                "{ $project : { _v : '$V', _id : 0 } }");

            var wrappedValueSerializer = outputSerializer.Should().BeOfType<WrappedValueSerializer<BsonValue>>().Subject;
            wrappedValueSerializer.ValueSerializer.Should().Be(BsonValueSerializer.Instance);

            var results = queryable.ToList();
            results.Should().Equal(expectedResult);
        }

        private IMongoCollection<Document<TValue>> CreateCollection<TValue>(TValue value)
        {
            var collection = GetCollection<Document<TValue>>();

            CreateCollection(
                collection,
                new Document<TValue> { V = value });

            return collection;
        }

        private TValue Deserialize<TValue>(IBsonSerializer<TValue> serializer, string json)
        {
            using (var reader = new JsonReader(json))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                return serializer.Deserialize(context);
            }
        }

        public class Document<TValue>
        {
            public TValue V { get; set; }
        }

        public class Anything
        {
            public int X { get; set; }
        }
    }
}
