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
using System.Text.RegularExpressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4191Tests
    {
        [Fact]
        public void AsBoolean_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBoolean == true);

            Assert(filter, "{ x : true }");
        }

        [Fact]
        public void AsBsonArray_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBsonArray == new BsonArray { 1, 2, 3 });

            Assert(filter, "{ x : [1, 2, 3] }");
        }

        [Fact]
        public void AsBsonBinaryData_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBsonBinaryData == new BsonBinaryData(new byte[] { 1, 2, 3 }));

            Assert(filter, "{ x : HexData(0, '010203') }");
        }

        [Fact]
        public void AsBsonDateTime_should_work()
        {
            var utc = new DateTime(2022, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBsonDateTime == new BsonDateTime(utc));

            Assert(filter, "{ x : ISODate('2022-01-02T03:04:05Z') }");
        }

        [Fact]
        public void AsBsonDocument_should_work()
        {
            var utc = DateTime.UtcNow;
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBsonDocument == new BsonDocument("y", 2));

            Assert(filter, "{ x : { y : 2 } }");
        }

        [Fact]
        public void AsBsonJavaScript_should_work()
        {
            var utc = DateTime.UtcNow;
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBsonJavaScript == new BsonJavaScript("code"));

            Assert(filter, "{ x : { $code : 'code'  } }");
        }

        [Fact]
        public void AsBsonJavaScriptWithScope_should_work()
        {
            var utc = DateTime.UtcNow;
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBsonJavaScriptWithScope == new BsonJavaScriptWithScope("code", new BsonDocument("y", 2)));

            Assert(filter, "{ x : { $code : 'code', $scope : { y : 2 }  } }");
        }

        [Fact]
        public void AsBsonMaxKey_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBsonMaxKey == BsonMaxKey.Value);

            Assert(filter, "{ x : { $maxKey : 1 } }");
        }

        [Fact]
        public void AsBsonMinKey_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBsonMinKey == BsonMinKey.Value);

            Assert(filter, "{ x : { $minKey : 1 } }");
        }

        [Fact]
        public void AsBsonNull_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBsonNull == BsonNull.Value);

            Assert(filter, "{ x : null }");
        }

        [Fact]
        public void AsBsonRegularExpression_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBsonRegularExpression == new BsonRegularExpression("abc", "i"));

            Assert(filter, "{ x : { $eq : /abc/i } }");
        }

        [Fact]
        public void AsBsonSymbol_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBsonSymbol == BsonSymbol.Create("abc"));

            Assert(filter, "{ x : { $symbol : 'abc' } }");
        }

        [Fact]
        public void AsBsonTimestamp_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBsonTimestamp == new BsonTimestamp(1, 2));

            Assert(filter, "{ x : Timestamp(1, 2) }");
        }

        [Fact]
        public void AsBsonUndefined_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBsonUndefined == BsonUndefined.Value);

            Assert(filter, "{ x : { $undefined : true } }");
        }

        [Fact]
        public void AsBsonValue_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsBsonValue == "abc");

            Assert(filter, "{ x : 'abc' }");
        }

        [Fact]
        public void AsByteArray_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsByteArray == new byte[] { 1, 2, 3 });

            Assert(filter, "{ x : HexData(0, '010203') }");
        }

        [Fact]
        public void AsDateTime_should_work()
        {
            var utc = new DateTime(2022, 1, 2, 4, 5, 6, DateTimeKind.Utc);
#pragma warning disable CS0618 // Type or member is obsolete
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsDateTime == utc);
#pragma warning restore CS0618 // Type or member is obsolete

            Assert(filter, "{ x : ISODate('2022-01-02T04:05:06Z') }");
        }

        [Fact]
        public void AsDecimal_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsDecimal == 1.3M);

            Assert(filter, "{ x : { $numberDecimal : '1.3' } }");
        }

        [Fact]
        public void AsDecimal128_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsDecimal128 == new Decimal128(1.3M));

            Assert(filter, "{ x : { $numberDecimal : '1.3' } }");
        }

        [Fact]
        public void AsDouble_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsDouble == 1.5);

            Assert(filter, "{ x : 1.5 }");
        }

        [Fact]
        public void AsGuid_should_work()
        {
            var value = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsGuid == value);

            Assert(filter, "{ x : HexData(4, '0102030405060708090a0b0c0d0e0f10') }");
        }

        [Fact]
        public void AsInt32_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsInt32 == 123);

            Assert(filter, "{ x : 123 }");
        }

        [Fact]
        public void AsInt64_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsInt64 == 123);

            Assert(filter, "{ x : { $numberLong : 123 } }");
        }

        [Fact]
        public void AsLocalTime_should_work()
        {
            var utc = new DateTime(2022, 1, 2, 4, 5, 6, DateTimeKind.Utc);
            var local = utc.ToLocalTime();
#pragma warning disable CS0618 // Type or member is obsolete
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsLocalTime == local);
#pragma warning restore CS0618 // Type or member is obsolete

            Assert(filter, "{ x : ISODate('2022-01-02T04:05:06Z') }");
        }

        [Fact]
        public void AsNullableBoolean_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsNullableBoolean == true);

            Assert(filter, "{ x : true }");
        }

        [Fact]
        public void AsNullableDateTime_should_work()
        {
            var utc = new DateTime(2022, 1, 2, 4, 5, 6, DateTimeKind.Utc);
#pragma warning disable CS0618 // Type or member is obsolete
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsNullableDateTime == utc);
#pragma warning restore CS0618 // Type or member is obsolete

            Assert(filter, "{ x : ISODate('2022-01-02T04:05:06Z') }");
        }

        [Fact]
        public void AsNullableDecimal_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsNullableDecimal == 1.3M);

            Assert(filter, "{ x : { $numberDecimal : '1.3' } }");
        }

        [Fact]
        public void AsNullableDecimal128_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsNullableDecimal128 == new Decimal128(1.3M));

            Assert(filter, "{ x : { $numberDecimal : '1.3' } }");
        }

        [Fact]
        public void AsNullableDouble_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsNullableDouble == 1.5);

            Assert(filter, "{ x : 1.5 }");
        }

        [Fact]
        public void AsNullableGuid_should_work()
        {
            var value = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsNullableGuid == value);

            Assert(filter, "{ x : HexData(4, '0102030405060708090a0b0c0d0e0f10') }");
        }

        [Fact]
        public void AsNullableInt32_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsNullableInt32 == 123);

            Assert(filter, "{ x : 123 }");
        }

        [Fact]
        public void AsNullableInt64_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsNullableInt64 == 123);

            Assert(filter, "{ x : { $numberLong : 123 } }");
        }

        [Fact]
        public void AsNullableObjectId_should_work()
        {
            var value = ObjectId.Parse("0102030405060708090a0b0c");
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsNullableObjectId == value);

            Assert(filter, "{ x : ObjectId('0102030405060708090a0b0c') }");
        }

        [Fact]
        public void AsObjectId_should_work()
        {
            var value = ObjectId.Parse("0102030405060708090a0b0c");
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsObjectId == value);

            Assert(filter, "{ x : ObjectId('0102030405060708090a0b0c') }");
        }

        [Fact]
        public void AsRegex_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsRegex == new Regex("abc", RegexOptions.IgnoreCase));

            Assert(filter, "{ x : { $eq : /abc/i } }");
        }

        [Fact]
        public void AsString_should_work()
        {
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsString == "abc");

            Assert(filter, "{ x : 'abc' }");
        }

        [Fact]
        public void AsUniversalTime_should_work()
        {
            var utc = new DateTime(2022, 1, 2, 4, 5, 6, DateTimeKind.Utc);
#pragma warning disable CS0618 // Type or member is obsolete
            var filter = Builders<BsonDocument>.Filter.Where(x => x["x"].AsUniversalTime == utc);
#pragma warning restore CS0618 // Type or member is obsolete

            Assert(filter, "{ x : ISODate('2022-01-02T04:05:06Z') }");
        }

        private void Assert(FilterDefinition<BsonDocument> filter, string expectedFilter)
        {
            var documentSerializer = BsonDocumentSerializer.Instance;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var rendered = filter.Render(new(documentSerializer, serializerRegistry));

            rendered.Should().Be(expectedFilter);
        }
    }
}
