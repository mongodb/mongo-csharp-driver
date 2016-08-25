/* Copyright 2010-2016 MongoDB Inc.
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
using System.Linq;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using FluentAssertions;
using Xunit;
using MongoDB.Bson.TestHelpers.XunitExtensions;

namespace MongoDB.Bson.Tests
{
    public class BsonValueTests
    {
        [Theory]
        [ParameterAttributeData]
        public void implicit_conversion_from_bool_should_return_precreated_instance(
            [Values(false, true)]
            bool value)
        {
            var result1 = (BsonValue)value;
            var result2 = (BsonValue)value;

            result2.Should().BeSameAs(result1);
        }

        [Theory]
        [ParameterAttributeData]
        public void implicit_conversion_from_double_should_return_new_instance(
            [Values(-101.0, 101.0)]
            double value)
        {
            var result1 = (BsonValue)value;
            var result2 = (BsonValue)value;

            result2.Should().NotBeSameAs(result1);
        }

        [Theory]
        [ParameterAttributeData]
        public void implicit_conversion_from_double_should_return_precreated_instance(
            [Range(-100.0, 100.0, 1.0)]
            double value)
        {
            var result1 = (BsonValue)value;
            var result2 = (BsonValue)value;

            result2.Should().BeSameAs(result1);
        }

        [Theory]
        [ParameterAttributeData]
        public void implicit_conversion_from_int_should_return_new_instance(
            [Values(-101, 101)]
            int value)
        {
            var result1 = (BsonValue)value;
            var result2 = (BsonValue)value;

            result2.Should().NotBeSameAs(result1);
        }

        [Theory]
        [ParameterAttributeData]
        public void implicit_conversion_from_int_should_return_precreated_instance(
            [Range(-100, 100)]
            int value)
        {
            var result1 = (BsonValue)value;
            var result2 = (BsonValue)value;

            result2.Should().BeSameAs(result1);
        }

        [Theory]
        [ParameterAttributeData]
        public void implicit_conversion_from_long_should_return_new_instance(
            [Values(-101L, 101L)]
            long value)
        {
            var result1 = (BsonValue)value;
            var result2 = (BsonValue)value;

            result2.Should().NotBeSameAs(result1);
        }

        [Theory]
        [ParameterAttributeData]
        public void implicit_conversion_from_long_should_return_precreated_instance(
            [Range(-100L, 100L, 1L)]
            long value)
        {
            var result1 = (BsonValue)value;
            var result2 = (BsonValue)value;

            result2.Should().BeSameAs(result1);
        }

        [Theory]
        [ParameterAttributeData]
        public void implicit_conversion_from_string_should_return_new_instance(
            [Values("x")]
            string value)
        {
            var result1 = (BsonValue)value;
            var result2 = (BsonValue)value;

            result2.Should().NotBeSameAs(result1);
        }

        [Theory]
        [ParameterAttributeData]
        public void implicit_conversion_from_string_should_return_precreated_instance(
            [Values("")]
            string value)
        {
            var result1 = (BsonValue)value;
            var result2 = (BsonValue)value;

            result2.Should().BeSameAs(result1);
        }

        [Fact]
        public void TestAsBoolean()
        {
            BsonValue v = true;
            BsonValue s = "";
            var b = v.AsBoolean;
            Assert.Equal(true, b);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBoolean; });
        }

        [Fact]
        public void TestAsBsonArray()
        {
            BsonValue v = new BsonArray { 1, 2 };
            BsonValue s = "";
            var a = v.AsBsonArray;
            Assert.Equal(2, a.Count);
            Assert.Equal(1, a[0].AsInt32);
            Assert.Equal(2, a[1].AsInt32);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonArray; });
        }

        [Fact]
        public void TestAsBinaryData()
        {
            BsonValue v = new byte[] { 1, 2 };
            BsonValue s = "";
            var b = v.AsBsonBinaryData;
            Assert.Equal(2, b.AsByteArray.Length);
            Assert.Equal(1, b.AsByteArray[0]);
            Assert.Equal(2, b.AsByteArray[1]);
            Assert.Equal(BsonBinarySubType.Binary, b.SubType);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonBinaryData; });
        }

        [Fact]
        public void TestAsBsonDocument()
        {
            BsonValue v = new BsonDocument("x", 1);
            BsonValue s = "";
            var d = v.AsBsonDocument;
            Assert.Equal(1, d.ElementCount);
            Assert.Equal("x", d.GetElement(0).Name);
            Assert.Equal(1, d[0].AsInt32);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonDocument; });
        }

        [Fact]
        public void TestAsBsonJavaScript()
        {
            BsonValue v = new BsonJavaScript("code");
            BsonValue s = "";
            var js = v.AsBsonJavaScript;
            Assert.Equal("code", js.Code);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonJavaScript; });
        }

        [Fact]
        public void TestAsBsonJavaScriptWithScode()
        {
            var scope = new BsonDocument("x", 1);
            BsonValue s = "";
            BsonValue v = new BsonJavaScriptWithScope("code", scope);
            var js = v.AsBsonJavaScriptWithScope;
            Assert.Equal("code", js.Code);
            Assert.Equal(1, js.Scope.ElementCount);
            Assert.Equal("x", js.Scope.GetElement(0).Name);
            Assert.Equal(1, js.Scope["x"].AsInt32);
            Assert.Same(v.AsBsonJavaScript, v.AsBsonJavaScriptWithScope);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonJavaScriptWithScope; });
        }

        [Fact]
        public void TestAsBsonMaxKey()
        {
            BsonValue v = BsonMaxKey.Value;
            BsonValue s = "";
            var m = v.AsBsonMaxKey;
            Assert.Same(BsonMaxKey.Value, m);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonMaxKey; });
        }

        [Fact]
        public void TestAsBsonMinKey()
        {
            BsonValue v = BsonMinKey.Value;
            BsonValue s = "";
            var m = v.AsBsonMinKey;
            Assert.Same(BsonMinKey.Value, m);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonMinKey; });
        }

        [Fact]
        public void TestAsBsonNull()
        {
            BsonValue v = BsonNull.Value;
            BsonValue s = "";
            var n = v.AsBsonNull;
            Assert.Same(BsonNull.Value, n);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonNull; });
        }

        [Fact]
        public void TestAsBsonRegularExpression()
        {
            BsonValue v = new BsonRegularExpression("pattern", "options");
            BsonValue s = "";
            var r = v.AsBsonRegularExpression;
            Assert.Equal("pattern", r.Pattern);
            Assert.Equal("options", r.Options);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonRegularExpression; });
        }

        [Fact]
        public void TestAsBsonSymbol()
        {
            BsonValue v = BsonSymbolTable.Lookup("name");
            BsonValue s = "";
            var sym = v.AsBsonSymbol;
            Assert.Equal("name", sym.Name);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonSymbol; });
        }

        [Fact]
        public void TestAsBsonTimestamp()
        {
            BsonValue v = new BsonTimestamp(1234);
            BsonValue s = "";
            var ts = v.AsBsonTimestamp;
            Assert.Equal(1234, ts.Value);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonTimestamp; });
        }

        [Fact]
        public void TestAsByteArray()
        {
            BsonValue v = new byte[] { 1, 2 };
            BsonValue s = "";
            var a = v.AsByteArray;
            Assert.Equal(2, a.Length);
            Assert.Equal(1, a[0]);
            Assert.Equal(2, a[1]);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsByteArray; });
        }

        [Fact]
        public void TestAsDateTime()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            BsonValue v = utcNow;
            BsonValue s = "";
            var dt = v.ToUniversalTime();
            Assert.Equal(utcNowTruncated, dt);
#pragma warning disable 618
            Assert.Throws<InvalidCastException>(() => { var x = s.AsDateTime; });
#pragma warning restore
            Assert.Throws<NotSupportedException>(() => s.ToUniversalTime());
        }

        [Fact]
        public void TestAsDecimal()
        {
            BsonValue v = (BsonDecimal128)1.5M;
            BsonValue s = "";
            var d = v.AsDecimal;
            Assert.Equal(1.5M, d);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsDecimal; });
        }

        [Fact]
        public void TestAsDecimal128()
        {
            BsonValue v = (BsonDecimal128)1.5M;
            BsonValue s = "";
            var d = v.AsDecimal128;
            Assert.Equal((Decimal128)1.5M, d);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsDecimal; });
        }

        [Fact]
        public void TestAsDouble()
        {
            BsonValue v = 1.5;
            BsonValue s = "";
            var d = v.AsDouble;
            Assert.Equal(1.5, d);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsDouble; });
        }

        [Fact]
        public void TestAsGuid()
        {
            var guid = Guid.NewGuid();
            BsonValue v = guid;
            BsonValue s = "";
            var g = v.AsGuid;
            Assert.Equal(guid, g);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsGuid; });
        }

        [Fact]
        public void TestAsInt32()
        {
            BsonValue v = 1;
            BsonValue s = "";
            var i = v.AsInt32;
            Assert.Equal(1, i);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsInt32; });
        }

        [Fact]
        public void TestAsInt64()
        {
            BsonValue v = 1L;
            BsonValue s = "";
            var i = v.AsInt64;
            Assert.Equal(1L, i);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsInt64; });
        }

        [Fact]
        public void TestAsNullableBoolean()
        {
            BsonValue v = true;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.Equal(true, v.AsNullableBoolean);
            Assert.Equal(null, n.AsNullableBoolean);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableBoolean; });
        }

        [Fact]
        public void TestAsNullableDateTime()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            BsonValue v = utcNow;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.Equal(utcNowTruncated, v.ToNullableUniversalTime());
            Assert.Equal(null, n.ToNullableUniversalTime());
#pragma warning disable 618
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableDateTime; });
#pragma warning restore
            Assert.Throws<NotSupportedException>(() => s.ToNullableUniversalTime());
        }

        [Fact]
        public void TestAsNullableDecimal()
        {
            BsonValue v = (BsonDecimal128)1.5M;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.Equal(1.5M, v.AsNullableDecimal);
            Assert.Equal(null, n.AsNullableDecimal);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableDecimal; });
        }

        [Fact]
        public void TestAsNullableDecimal128()
        {
            BsonValue v = (BsonDecimal128)1.5M;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.Equal((Decimal128)1.5M, v.AsNullableDecimal128);
            Assert.Equal(null, n.AsNullableDecimal128);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableDecimal128; });
        }

        [Fact]
        public void TestAsNullableDouble()
        {
            BsonValue v = 1.5;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.Equal(1.5, v.AsNullableDouble);
            Assert.Equal(null, n.AsNullableDouble);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableDouble; });
        }

        [Fact]
        public void TestAsNullableGuid()
        {
            Guid guid = Guid.NewGuid();
            BsonValue v = guid;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.Equal(guid, v.AsNullableGuid);
            Assert.Equal(null, n.AsNullableGuid);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableGuid; });
        }

        [Fact]
        public void TestAsNullableInt32()
        {
            BsonValue v = 1;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.Equal(1, v.AsNullableInt32);
            Assert.Equal(null, n.AsNullableInt32);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableInt32; });
        }

        [Fact]
        public void TestAsNullableInt64()
        {
            BsonValue v = 1L;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.Equal(1L, v.AsNullableInt64);
            Assert.Equal(null, n.AsNullableInt64);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableInt64; });
        }

        [Fact]
        public void TestAsNullableObjectId()
        {
            var objectId = ObjectId.GenerateNewId();
            BsonValue v = objectId;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.Equal(objectId, v.AsNullableObjectId);
            Assert.Equal(null, n.AsNullableObjectId);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableObjectId; });
        }

        [Fact]
        public void TestAsObjectId()
        {
            var objectId = ObjectId.GenerateNewId();
            BsonValue v = objectId;
            BsonValue s = "";
            var o = v.AsObjectId;
            Assert.Equal(objectId, o);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsObjectId; });
        }

        [Fact]
        public void TestAsRegexOptionNone()
        {
            BsonValue v = new BsonRegularExpression("xyz");
            BsonValue s = "";
            var r = v.AsRegex;
            Assert.Equal(RegexOptions.None, r.Options);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsRegex; });
        }

        [Fact]
        public void TestAsRegexOptionAll()
        {
            BsonValue v = new BsonRegularExpression("xyz", "imxs");
            BsonValue s = "";
            var r = v.AsRegex;
            Assert.Equal(RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline, r.Options);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsRegex; });
        }

        [Fact]
        public void TestAsRegexOptionI()
        {
            BsonValue v = new BsonRegularExpression("xyz", "i");
            BsonValue s = "";
            var r = v.AsRegex;
            Assert.Equal(RegexOptions.IgnoreCase, r.Options);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsRegex; });
        }

        [Fact]
        public void TestAsRegexOptionM()
        {
            BsonValue v = new BsonRegularExpression("xyz", "m");
            BsonValue s = "";
            var r = v.AsRegex;
            Assert.Equal(RegexOptions.Multiline, r.Options);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsRegex; });
        }

        [Fact]
        public void TestAsRegexOptionX()
        {
            BsonValue v = new BsonRegularExpression("xyz", "x");
            BsonValue s = "";
            var r = v.AsRegex;
            Assert.Equal(RegexOptions.IgnorePatternWhitespace, r.Options);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsRegex; });
        }

        [Fact]
        public void TestAsRegexOptionS()
        {
            BsonValue v = new BsonRegularExpression("xyz", "s");
            BsonValue s = "";
            var r = v.AsRegex;
            Assert.Equal(RegexOptions.Singleline, r.Options);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsRegex; });
        }

        [Fact]
        public void TestAsString()
        {
            BsonValue v = "Hello";
            BsonValue i = 1;
            var s = v.AsString;
            Assert.Equal("Hello", s);
            Assert.Throws<InvalidCastException>(() => { var x = i.AsString; });
        }

        [Fact]
        public void TestBsonRegularExpressionConstructors()
        {
            var regex = new BsonRegularExpression("pattern");
            Assert.IsType<BsonRegularExpression>(regex);
            Assert.Equal("pattern", regex.Pattern);
            Assert.Equal("", regex.Options);

            regex = new BsonRegularExpression("/pattern/i");
            Assert.IsType<BsonRegularExpression>(regex);
            Assert.Equal("pattern", regex.Pattern);
            Assert.Equal("i", regex.Options);

            regex = new BsonRegularExpression(@"/pattern\/withslash/i");
            Assert.IsType<BsonRegularExpression>(regex);
            Assert.Equal("pattern/withslash", regex.Pattern);
            Assert.Equal("i", regex.Options);

            regex = new BsonRegularExpression("pattern", "i");
            Assert.IsType<BsonRegularExpression>(regex);
            Assert.Equal("pattern", regex.Pattern);
            Assert.Equal("i", regex.Options);

            regex = new BsonRegularExpression(new Regex("pattern"));
            Assert.IsType<BsonRegularExpression>(regex);
            Assert.Equal("pattern", regex.Pattern);
            Assert.Equal("", regex.Options);

            regex = new BsonRegularExpression(new Regex("pattern", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline));
            Assert.IsType<BsonRegularExpression>(regex);
            Assert.Equal("pattern", regex.Pattern);
            Assert.Equal("imxs", regex.Options);

            regex = new BsonRegularExpression(new Regex("pattern", RegexOptions.IgnoreCase));
            Assert.IsType<BsonRegularExpression>(regex);
            Assert.Equal("pattern", regex.Pattern);
            Assert.Equal("i", regex.Options);

            regex = new BsonRegularExpression(new Regex("pattern", RegexOptions.Multiline));
            Assert.IsType<BsonRegularExpression>(regex);
            Assert.Equal("pattern", regex.Pattern);
            Assert.Equal("m", regex.Options);

            regex = new BsonRegularExpression(new Regex("pattern", RegexOptions.IgnorePatternWhitespace));
            Assert.IsType<BsonRegularExpression>(regex);
            Assert.Equal("pattern", regex.Pattern);
            Assert.Equal("x", regex.Options);

            regex = new BsonRegularExpression(new Regex("pattern", RegexOptions.Singleline));
            Assert.IsType<BsonRegularExpression>(regex);
            Assert.Equal("pattern", regex.Pattern);
            Assert.Equal("s", regex.Options);
        }

        [Fact]
        public void TestBsonValueEqualsFalse()
        {
            BsonValue a = false;
            Assert.True(a == false);
            Assert.False(a != false);
            Assert.False(a == true);
            Assert.True(a != true);
        }

        [Fact]
        public void TestBsonValueEqualsTrue()
        {
            BsonValue a = true;
            Assert.True(a == true);
            Assert.False(a != true);
            Assert.False(a == false);
            Assert.True(a != false);
        }

        [Fact]
        public void TestBsonValueEqualsDouble()
        {
            BsonValue a = 1;
            Assert.True(a == 1.0);
            Assert.False(a != 1.0);
            Assert.False(a == 2.0);
            Assert.True(a != 2.0);
        }

        [Fact]
        public void TestBsonValueEqualsInt32()
        {
            BsonValue a = 1;
            Assert.True(a == 1);
            Assert.False(a != 1);
            Assert.False(a == 2);
            Assert.True(a != 2);
        }

        [Fact]
        public void TestBsonValueEqualsInt64()
        {
            BsonValue a = 1;
            Assert.True(a == 1);
            Assert.False(a != 1);
            Assert.False(a == 2);
            Assert.True(a != 2);
        }

        [Fact]
        public void TestCreateNull()
        {
            object obj = null;
            Assert.Same(BsonNull.Value, BsonValue.Create(obj));
        }

        [Fact]
        public void TestExplicitConversionToDecimal()
        {
            BsonValue v = 1.5M;
            var r = (decimal)v;
            Assert.Equal(1.5M, r);
        }

        [Fact]
        public void TestExplicitConversionToDecimal128()
        {
            BsonValue v = 1.5M;
            var r = (Decimal128)v;
            Assert.Equal((Decimal128)1.5M, r);
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(null)]
        public void TestExplicitConversionToNullableDecimal(double? nullableDoubleValue)
        {
            var nullableDecimalValue = nullableDoubleValue == null ? (decimal?)null : (decimal)nullableDoubleValue;
            BsonValue v = nullableDecimalValue;
            var r = (decimal?)v;
            Assert.Equal(nullableDecimalValue, r);
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(null)]
        public void TestExplicitConversionToNullableDecimal128(double? nullableDoubleValue)
        {
            var nullableDecimal128Value = nullableDoubleValue == null ? (Decimal128?)null : (Decimal128)(decimal)nullableDoubleValue;
            BsonValue v = nullableDecimal128Value;
            var r = (Decimal128?)v;
            Assert.Equal(nullableDecimal128Value, r);
        }

        [Fact]
        public void TestImplicitConversionFromBoolean()
        {
            BsonValue v = true;
            Assert.IsType<BsonBoolean>(v);
            var b = (BsonBoolean)v;
            Assert.Equal(true, b.Value);
        }

        [Fact]
        public void TestImplicitConversionFromByteArray()
        {
            BsonValue v = new byte[] { 1, 2 };
            BsonValue n = (byte[])null;
            Assert.IsType<BsonBinaryData>(v);
            Assert.Null(n);
            var b = (BsonBinaryData)v;
            Assert.Equal(BsonBinarySubType.Binary, b.SubType);
            Assert.Equal(1, b.AsByteArray[0]);
            Assert.Equal(2, b.AsByteArray[1]);
        }

        [Fact]
        public void TestImplicitConversionFromDateTime()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            BsonValue v = utcNow;
            Assert.IsType<BsonDateTime>(v);
            var dt = (BsonDateTime)v;
            Assert.Equal(utcNowTruncated, dt.ToUniversalTime());
        }

        [Fact]
        public void TestImplicitConversionFromDecimal()
        {
            BsonValue v = 1.5M;
            Assert.IsType<BsonDecimal128>(v);
            var d = (BsonDecimal128)v;
            Assert.Equal((Decimal128)1.5M, d.Value);
        }

        [Fact]
        public void TestImplicitConversionFromDecimal128()
        {
            BsonValue v = (Decimal128)1.5M;
            Assert.IsType<BsonDecimal128>(v);
            var d = (BsonDecimal128)v;
            Assert.Equal((Decimal128)1.5M, d.Value);
        }

        [Fact]
        public void TestImplicitConversionFromDouble()
        {
            BsonValue v = 1.5;
            Assert.IsType<BsonDouble>(v);
            var d = (BsonDouble)v;
            Assert.Equal(1.5, d.Value);
        }

        [Fact]
        public void TestImplicitConversionFromGuid()
        {
            var guid = Guid.NewGuid();
            BsonValue v = guid;
            Assert.IsType<BsonBinaryData>(v);
            var b = (BsonBinaryData)v;
            Assert.True(guid.ToByteArray().SequenceEqual(b.AsByteArray));
            Assert.Equal(BsonBinarySubType.UuidLegacy, b.SubType);
        }

        [Fact]
        public void TestImplicitConversionFromInt16Enum()
        {
            BsonValue v = Int16Enum.A;
            Assert.IsType<BsonInt32>(v);
            var n = (BsonInt32)v;
            Assert.Equal((int)Int16Enum.A, n.Value);
        }

        [Fact]
        public void TestImplicitConversionFromInt32()
        {
            BsonValue v = 1;
            Assert.IsType<BsonInt32>(v);
            var i = (BsonInt32)v;
            Assert.Equal(1, i.Value);
        }

        [Fact]
        public void TestImplicitConversionFromInt32Enum()
        {
            BsonValue v = Int32Enum.A;
            Assert.IsType<BsonInt32>(v);
            var n = (BsonInt32)v;
            Assert.Equal((int)Int32Enum.A, n.Value);
        }

        [Fact]
        public void TestImplicitConversionFromInt64()
        {
            BsonValue v = 1L;
            Assert.IsType<BsonInt64>(v);
            var i = (BsonInt64)v;
            Assert.Equal(1L, i.Value);
        }

        [Fact]
        public void TestImplicitConversionFromInt64Enum()
        {
            BsonValue v = Int64Enum.A;
            Assert.IsType<BsonInt64>(v);
            var n = (BsonInt64)v;
            Assert.Equal((int)Int64Enum.A, n.Value);
        }

        [Fact]
        public void TestImplicitConversionFromNullableBoolean()
        {
            BsonValue v = (bool?)true;
            BsonValue n = (bool?)null;
            Assert.IsType<BsonBoolean>(v);
            Assert.IsType<BsonNull>(n);
            var b = (BsonBoolean)v;
            Assert.Equal(true, b.Value);
        }

        [Fact]
        public void TestImplicitConversionFromNullableDateTime()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            BsonValue v = (DateTime?)utcNow;
            BsonValue n = (DateTime?)null;
            Assert.IsType<BsonDateTime>(v);
            Assert.IsType<BsonNull>(n);
            var dt = (BsonDateTime)v;
            Assert.Equal(utcNowTruncated, dt.ToUniversalTime());
        }

        [Fact]
        public void TestImplicitConversionFromNullableDecimal()
        {
            BsonValue v = (decimal?)1.5M;
            BsonValue n = (decimal?)null;
            Assert.IsType<BsonDecimal128>(v);
            Assert.IsType<BsonNull>(n);
            var d = (BsonDecimal128)v;
            Assert.Equal((Decimal128)1.5M, d.Value);
        }

        [Fact]
        public void TestImplicitConversionFromNullableDecimal128()
        {
            BsonValue v = (Decimal128?)1.5M;
            BsonValue n = (Decimal128?)null;
            Assert.IsType<BsonDecimal128>(v);
            Assert.IsType<BsonNull>(n);
            var d = (BsonDecimal128)v;
            Assert.Equal((Decimal128)1.5M, d.Value);
        }

        [Fact]
        public void TestImplicitConversionFromNullableDouble()
        {
            BsonValue v = (double?)1.5;
            BsonValue n = (double?)null;
            Assert.IsType<BsonDouble>(v);
            Assert.IsType<BsonNull>(n);
            var d = (BsonDouble)v;
            Assert.Equal(1.5, d.Value);
        }

        [Fact]
        public void TestImplicitConversionFromNullableGuid()
        {
            var guid = Guid.NewGuid();
            BsonValue v = (Guid?)guid;
            BsonValue n = (Guid?)null;
            Assert.IsType<BsonBinaryData>(v);
            Assert.IsType<BsonNull>(n);
            var b = (BsonBinaryData)v;
            Assert.True(guid.ToByteArray().SequenceEqual(b.AsByteArray));
            Assert.Equal(BsonBinarySubType.UuidLegacy, b.SubType);
        }

        [Fact]
        public void TestImplicitConversionFromNullableInt32()
        {
            BsonValue v = (int?)1;
            BsonValue n = (int?)null;
            Assert.IsType<BsonInt32>(v);
            Assert.IsType<BsonNull>(n);
            var i = (BsonInt32)v;
            Assert.Equal(1, i.Value);
        }

        [Fact]
        public void TestImplicitConversionFromNullableInt64()
        {
            BsonValue v = (long?)1L;
            BsonValue n = (long?)null;
            Assert.IsType<BsonInt64>(v);
            Assert.IsType<BsonNull>(n);
            var i = (BsonInt64)v;
            Assert.Equal(1, i.Value);
        }

        [Fact]
        public void TestImplicitConversionFromNullableObjectId()
        {
            var objectId = ObjectId.GenerateNewId();
            BsonValue v = objectId;
            BsonValue n = (Guid?)null;
            Assert.IsType<BsonObjectId>(v);
            Assert.IsType<BsonNull>(n);
            var o = (BsonObjectId)v;
            Assert.Equal(objectId, o.Value);
        }

        [Fact]
        public void TestImplicitConversionFromObjectId()
        {
            var objectId = ObjectId.GenerateNewId();
            BsonValue v = objectId;
            Assert.IsType<BsonObjectId>(v);
            var o = (BsonObjectId)v;
            Assert.Equal(objectId, o.Value);
        }

        [Fact]
        public void TestImplicitConversionFromRegexOptionNone()
        {
            BsonValue v = new Regex("xyz");
            BsonValue n = (Regex)null;
            Assert.IsType<BsonRegularExpression>(v);
            Assert.Null(n);
            var r = (BsonRegularExpression)v;
            Assert.Equal("xyz", r.Pattern);
            Assert.Equal("", r.Options);
        }

        [Fact]
        public void TestImplicitConversionFromRegexOptionAll()
        {
            BsonValue v = new Regex("xyz", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            BsonValue n = (Regex)null;
            Assert.IsType<BsonRegularExpression>(v);
            Assert.Null(n);
            var r = (BsonRegularExpression)v;
            Assert.Equal("xyz", r.Pattern);
            Assert.Equal("imxs", r.Options);
        }

        [Fact]
        public void TestImplicitConversionFromRegexOptionI()
        {
            BsonValue v = new Regex("xyz", RegexOptions.IgnoreCase);
            BsonValue n = (Regex)null;
            Assert.IsType<BsonRegularExpression>(v);
            Assert.Null(n);
            var r = (BsonRegularExpression)v;
            Assert.Equal("xyz", r.Pattern);
            Assert.Equal("i", r.Options);
        }

        [Fact]
        public void TestImplicitConversionFromRegexOptionM()
        {
            BsonValue v = new Regex("xyz", RegexOptions.Multiline);
            BsonValue n = (Regex)null;
            Assert.IsType<BsonRegularExpression>(v);
            Assert.Null(n);
            var r = (BsonRegularExpression)v;
            Assert.Equal("xyz", r.Pattern);
            Assert.Equal("m", r.Options);
        }

        [Fact]
        public void TestImplicitConversionFromRegexOptionX()
        {
            BsonValue v = new Regex("xyz", RegexOptions.IgnorePatternWhitespace);
            BsonValue n = (Regex)null;
            Assert.IsType<BsonRegularExpression>(v);
            Assert.Null(n);
            var r = (BsonRegularExpression)v;
            Assert.Equal("xyz", r.Pattern);
            Assert.Equal("x", r.Options);
        }

        [Fact]
        public void TestImplicitConversionFromRegexOptionS()
        {
            BsonValue v = new Regex("xyz", RegexOptions.Singleline);
            BsonValue n = (Regex)null;
            Assert.IsType<BsonRegularExpression>(v);
            Assert.Null(n);
            var r = (BsonRegularExpression)v;
            Assert.Equal("xyz", r.Pattern);
            Assert.Equal("s", r.Options);
        }

        [Fact]
        public void TestImplicitConversionFromString()
        {
            BsonValue v = "xyz";
            BsonValue n = (string)null;
            Assert.IsType<BsonString>(v);
            Assert.Null(n);
            var s = (BsonString)v;
            Assert.Equal("xyz", s.Value);
        }

        [Fact]
        public void TestIsDecimal128()
        {
            BsonValue v = new BsonDecimal128(1.0M);
            BsonValue s = new BsonString("");
            Assert.True(v.IsDecimal128);
            Assert.False(s.IsDecimal128);
        }

        [Fact]
        public void TestIsNumeric()
        {
            BsonValue d128 = new BsonDecimal128(1.0M);
            BsonValue d = new BsonDouble(1.0);
            BsonValue i32 = new BsonInt32(1);
            BsonValue i64 = new BsonInt64(1L);
            BsonValue s = new BsonString("");
            Assert.True(d128.IsNumeric);
            Assert.True(d.IsNumeric);
            Assert.True(i32.IsNumeric);
            Assert.True(i64.IsNumeric);
            Assert.False(s.IsDecimal128);
        }

        // nested types
        public enum Int16Enum : short
        {
            A = 1,
            B
        }

        public enum Int32Enum
        {
            A = 1,
            B
        }

        public enum Int64Enum : long
        {
            A = 1,
            B
        }
    }
}
