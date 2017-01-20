/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BooleanSerializerTests
    {
        public class TestClass
        {
            public bool N;
            [BsonRepresentation(BsonType.Boolean)]
            public bool B;
            [BsonRepresentation(BsonType.Decimal128)]
            public bool D128;
            [BsonRepresentation(BsonType.Double)]
            public bool D;
            [BsonRepresentation(BsonType.Int32)]
            public bool I;
            [BsonRepresentation(BsonType.Int64)]
            public bool L;
            [BsonRepresentation(BsonType.String)]
            public bool S;
        }

        [Fact]
        public void TestFalse()
        {
            var obj = new TestClass
            {
                N = false, B = false, D128 = false, D = false, I = false, L = false, S = false
            };
            var json = obj.ToJson();
            var expected = "{ 'N' : false, 'B' : false, 'D128' : NumberDecimal('0'), 'D' : 0.0, 'I' : 0, 'L' : NumberLong(0), 'S' : 'false' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestTrue()
        {
            var obj = new TestClass
            {
                N = true, B = true, D128 = true, D = true, I = true, L = true, S = true
            };
            var json = obj.ToJson();
            var expected = "{ 'N' : true, 'B' : true, 'D128' : NumberDecimal('1'), 'D' : 1.0, 'I' : 1, 'L' : NumberLong(1), 'S' : 'true' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class DateTimeSerializerTests
    {
        public class TestClass
        {
            public DateTime Default;
            [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
            public DateTime Local;
            [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified)]
            public DateTime Unspecified;
            [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
            public DateTime Utc;
            [BsonDateTimeOptions(Representation = BsonType.Int64)]
            public DateTime Ticks;
            [BsonDateTimeOptions(Representation = BsonType.String)]
            public DateTime String;
            [BsonDateTimeOptions(Representation = BsonType.String, DateOnly = true)]
            public DateTime DateOnlyString;
            [BsonDateTimeOptions(Representation = BsonType.Document)]
            public DateTime Document;
        }

        private static string __expectedTemplate =
            "{ 'Default' : #Default, 'Local' : #Local, 'Unspecified' : #Unspecified, 'Utc' : #Utc, 'Ticks' : #Ticks, 'String' : '#String', 'DateOnlyString' : '#DateOnlyString', 'Document' : #Document }";

        [Fact]
        public void TestMinLocal()
        {
            var minLocal = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local);
            var obj = new TestClass
            {
                Default = minLocal,
                Local = minLocal,
                Unspecified = minLocal,
                Utc = minLocal,
                Ticks = minLocal,
                String = minLocal,
                DateOnlyString = minLocal.Date,
                Document = minLocal
            };
            var json = obj.ToJson();
            var expected = __expectedTemplate;
            expected = expected.Replace("#Default", "ISODate('0001-01-01T00:00:00Z')");
            expected = expected.Replace("#Local", "ISODate('0001-01-01T00:00:00Z')");
            expected = expected.Replace("#Unspecified", "ISODate('0001-01-01T00:00:00Z')");
            expected = expected.Replace("#Utc", "ISODate('0001-01-01T00:00:00Z')");
            expected = expected.Replace("#Ticks", "NumberLong(0)");
            expected = expected.Replace("#String", "0001-01-01T00:00:00");
            expected = expected.Replace("#DateOnlyString", "0001-01-01");
            expected = expected.Replace("#Document", "{ 'DateTime' : ISODate('0001-01-01T00:00:00Z'), 'Ticks' : NumberLong(0) }");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(DateTime.MinValue, rehydrated.Default);
            Assert.Equal(DateTime.MinValue, rehydrated.Local);
            Assert.Equal(DateTime.MinValue, rehydrated.Unspecified);
            Assert.Equal(DateTime.MinValue, rehydrated.Utc);
            Assert.Equal(DateTime.MinValue, rehydrated.Ticks);
            Assert.Equal(DateTime.MinValue, rehydrated.String);
            Assert.Equal(DateTime.MinValue.Date, rehydrated.DateOnlyString);
            Assert.Equal(DateTime.MinValue, rehydrated.Document);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.Equal(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.Equal(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Document.Kind);
        }

        [Fact]
        public void TestMinUnspecified()
        {
            var minUnspecified = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified);
            var obj = new TestClass
            {
                Default = minUnspecified,
                Local = minUnspecified,
                Unspecified = minUnspecified,
                Utc = minUnspecified,
                Ticks = minUnspecified,
                String = minUnspecified,
                DateOnlyString = minUnspecified.Date,
                Document = minUnspecified
            };
            var json = obj.ToJson();
            var expected = __expectedTemplate;
            expected = expected.Replace("#Default", "ISODate('0001-01-01T00:00:00Z')");
            expected = expected.Replace("#Local", "ISODate('0001-01-01T00:00:00Z')");
            expected = expected.Replace("#Unspecified", "ISODate('0001-01-01T00:00:00Z')");
            expected = expected.Replace("#Utc", "ISODate('0001-01-01T00:00:00Z')");
            expected = expected.Replace("#Ticks", "NumberLong(0)");
            expected = expected.Replace("#String", "0001-01-01T00:00:00");
            expected = expected.Replace("#DateOnlyString", "0001-01-01");
            expected = expected.Replace("#Document", "{ 'DateTime' : ISODate('0001-01-01T00:00:00Z'), 'Ticks' : NumberLong(0) }");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(DateTime.MinValue, rehydrated.Default);
            Assert.Equal(DateTime.MinValue, rehydrated.Local);
            Assert.Equal(DateTime.MinValue, rehydrated.Unspecified);
            Assert.Equal(DateTime.MinValue, rehydrated.Utc);
            Assert.Equal(DateTime.MinValue, rehydrated.Ticks);
            Assert.Equal(DateTime.MinValue, rehydrated.String);
            Assert.Equal(DateTime.MinValue.Date, rehydrated.DateOnlyString);
            Assert.Equal(DateTime.MinValue, rehydrated.Document);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.Equal(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.Equal(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Document.Kind);
        }

        [Fact]
        public void TestMinUtc()
        {
            var minUtc = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            var obj = new TestClass
            {
                Default = minUtc,
                Local = minUtc,
                Unspecified = minUtc,
                Utc = minUtc,
                Ticks = minUtc,
                String = minUtc,
                DateOnlyString = minUtc.Date,
                Document = minUtc
            };
            var json = obj.ToJson();
            var expected = __expectedTemplate;
            expected = expected.Replace("#Default", "ISODate('0001-01-01T00:00:00Z')");
            expected = expected.Replace("#Local", "ISODate('0001-01-01T00:00:00Z')");
            expected = expected.Replace("#Unspecified", "ISODate('0001-01-01T00:00:00Z')");
            expected = expected.Replace("#Utc", "ISODate('0001-01-01T00:00:00Z')");
            expected = expected.Replace("#Ticks", "NumberLong(0)");
            expected = expected.Replace("#String", "0001-01-01T00:00:00");
            expected = expected.Replace("#DateOnlyString", "0001-01-01");
            expected = expected.Replace("#Document", "{ 'DateTime' : ISODate('0001-01-01T00:00:00Z'), 'Ticks' : NumberLong(0) }");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(DateTime.MinValue, rehydrated.Default);
            Assert.Equal(DateTime.MinValue, rehydrated.Local);
            Assert.Equal(DateTime.MinValue, rehydrated.Unspecified);
            Assert.Equal(DateTime.MinValue, rehydrated.Utc);
            Assert.Equal(DateTime.MinValue, rehydrated.Ticks);
            Assert.Equal(DateTime.MinValue, rehydrated.String);
            Assert.Equal(DateTime.MinValue.Date, rehydrated.DateOnlyString);
            Assert.Equal(DateTime.MinValue, rehydrated.Document);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.Equal(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.Equal(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Document.Kind);
        }

        [Fact]
        public void TestMaxLocal()
        {
            var maxLocal = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local);
            var obj = new TestClass
            {
                Default = maxLocal,
                Local = maxLocal,
                Unspecified = maxLocal,
                Utc = maxLocal,
                Ticks = maxLocal,
                String = maxLocal,
                DateOnlyString = maxLocal.Date,
                Document = maxLocal
            };
            var json = obj.ToJson();
            var expected = __expectedTemplate;
            expected = expected.Replace("#Default", "ISODate('9999-12-31T23:59:59.999Z')");
            expected = expected.Replace("#Local", "ISODate('9999-12-31T23:59:59.999Z')");
            expected = expected.Replace("#Unspecified", "ISODate('9999-12-31T23:59:59.999Z')");
            expected = expected.Replace("#Utc", "ISODate('9999-12-31T23:59:59.999Z')");
            expected = expected.Replace("#Ticks", "NumberLong('3155378975999999999')");
            expected = expected.Replace("#String", "9999-12-31T23:59:59.9999999");
            expected = expected.Replace("#DateOnlyString", "9999-12-31");
            expected = expected.Replace("#Document", "{ 'DateTime' : ISODate('9999-12-31T23:59:59.999Z'), 'Ticks' : NumberLong('3155378975999999999') }");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(DateTime.MaxValue, rehydrated.Default);
            Assert.Equal(DateTime.MaxValue, rehydrated.Local);
            Assert.Equal(DateTime.MaxValue, rehydrated.Unspecified);
            Assert.Equal(DateTime.MaxValue, rehydrated.Utc);
            Assert.Equal(DateTime.MaxValue, rehydrated.Ticks);
            Assert.Equal(DateTime.MaxValue, rehydrated.String);
            Assert.Equal(DateTime.MaxValue.Date, rehydrated.DateOnlyString);
            Assert.Equal(DateTime.MaxValue, rehydrated.Document);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.Equal(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.Equal(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Document.Kind);
        }

        [Fact]
        public void TestMaxUnspecified()
        {
            var maxUnspecified = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Unspecified);
            var obj = new TestClass
            {
                Default = maxUnspecified,
                Local = maxUnspecified,
                Unspecified = maxUnspecified,
                Utc = maxUnspecified,
                Ticks = maxUnspecified,
                String = maxUnspecified,
                DateOnlyString = maxUnspecified.Date,
                Document = maxUnspecified
            };
            var json = obj.ToJson();
            var expected = __expectedTemplate;
            expected = expected.Replace("#Default", "ISODate('9999-12-31T23:59:59.999Z')");
            expected = expected.Replace("#Local", "ISODate('9999-12-31T23:59:59.999Z')");
            expected = expected.Replace("#Unspecified", "ISODate('9999-12-31T23:59:59.999Z')");
            expected = expected.Replace("#Utc", "ISODate('9999-12-31T23:59:59.999Z')");
            expected = expected.Replace("#Ticks", "NumberLong('3155378975999999999')");
            expected = expected.Replace("#String", "9999-12-31T23:59:59.9999999");
            expected = expected.Replace("#DateOnlyString", "9999-12-31");
            expected = expected.Replace("#Document", "{ 'DateTime' : ISODate('9999-12-31T23:59:59.999Z'), 'Ticks' : NumberLong('3155378975999999999') }");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(DateTime.MaxValue, rehydrated.Default);
            Assert.Equal(DateTime.MaxValue, rehydrated.Local);
            Assert.Equal(DateTime.MaxValue, rehydrated.Unspecified);
            Assert.Equal(DateTime.MaxValue, rehydrated.Utc);
            Assert.Equal(DateTime.MaxValue, rehydrated.Ticks);
            Assert.Equal(DateTime.MaxValue, rehydrated.String);
            Assert.Equal(DateTime.MaxValue.Date, rehydrated.DateOnlyString);
            Assert.Equal(DateTime.MaxValue, rehydrated.Document);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.Equal(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.Equal(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Document.Kind);
        }

        [Fact]
        public void TestMaxUtc()
        {
            var maxUtc = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            var obj = new TestClass
            {
                Default = maxUtc,
                Local = maxUtc,
                Unspecified = maxUtc,
                Utc = maxUtc,
                Ticks = maxUtc,
                String = maxUtc,
                DateOnlyString = maxUtc.Date,
                Document = maxUtc
            };
            var json = obj.ToJson();
            var expected = __expectedTemplate;
            expected = expected.Replace("#Default", "ISODate('9999-12-31T23:59:59.999Z')");
            expected = expected.Replace("#Local", "ISODate('9999-12-31T23:59:59.999Z')");
            expected = expected.Replace("#Unspecified", "ISODate('9999-12-31T23:59:59.999Z')");
            expected = expected.Replace("#Utc", "ISODate('9999-12-31T23:59:59.999Z')");
            expected = expected.Replace("#Ticks", "NumberLong('3155378975999999999')");
            expected = expected.Replace("#String", "9999-12-31T23:59:59.9999999");
            expected = expected.Replace("#DateOnlyString", "9999-12-31");
            expected = expected.Replace("#Document", "{ 'DateTime' : ISODate('9999-12-31T23:59:59.999Z'), 'Ticks' : NumberLong('3155378975999999999') }");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.Equal(DateTime.MaxValue, rehydrated.Default);
            Assert.Equal(DateTime.MaxValue, rehydrated.Local);
            Assert.Equal(DateTime.MaxValue, rehydrated.Unspecified);
            Assert.Equal(DateTime.MaxValue, rehydrated.Utc);
            Assert.Equal(DateTime.MaxValue, rehydrated.Ticks);
            Assert.Equal(DateTime.MaxValue, rehydrated.String);
            Assert.Equal(DateTime.MaxValue.Date, rehydrated.DateOnlyString);
            Assert.Equal(DateTime.MaxValue, rehydrated.Document);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.Equal(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.Equal(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Document.Kind);
        }

        [Fact]
        public void TestLocal()
        {
            var local = DateTime.Now;
            var utc = local.ToUniversalTime();
            var obj = new TestClass
            {
                Default = local,
                Local = local,
                Unspecified = local,
                Utc = local,
                Ticks = local,
                String = local,
                DateOnlyString = local.Date,
                Document = local
            };
            var json = obj.ToJson();
            var expected = __expectedTemplate;
            var milliseconds = (long)(utc - BsonConstants.UnixEpoch).TotalMilliseconds;
            var utcJson = string.Format("ISODate(\"{0}\")", utc.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"));
            expected = expected.Replace("#Default", utcJson);
            expected = expected.Replace("#Local", utcJson);
            expected = expected.Replace("#Unspecified", utcJson);
            expected = expected.Replace("#Utc", utcJson);
            expected = expected.Replace("#Ticks", string.Format("NumberLong('{0}')", utc.Ticks.ToString()));
            expected = expected.Replace("#String", local.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz"));
            expected = expected.Replace("#DateOnlyString", local.Date.ToString("yyyy-MM-dd"));
            expected = expected.Replace("#Document", "{ 'DateTime' : #D, 'Ticks' : NumberLong('#T') }".Replace("#D", utcJson).Replace("#T", utc.Ticks.ToString()));
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            var utcTruncated = BsonConstants.UnixEpoch.AddMilliseconds(milliseconds); // loss of precision
            var localTruncated = utcTruncated.ToLocalTime();
            Assert.Equal(utcTruncated, rehydrated.Default);
            Assert.Equal(localTruncated, rehydrated.Local);
            Assert.Equal(localTruncated, rehydrated.Unspecified);
            Assert.Equal(utcTruncated, rehydrated.Utc);
            Assert.Equal(utc, rehydrated.Ticks);
            Assert.Equal(utc, rehydrated.String);
            Assert.Equal(local.Date, rehydrated.DateOnlyString);
            Assert.Equal(utc, rehydrated.Document);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.Equal(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.Equal(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Document.Kind);
        }

        [Fact]
        public void TestUnspecified()
        {
            var unspecified = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            var utc = unspecified.ToUniversalTime();
            var obj = new TestClass
            {
                Default = unspecified,
                Local = unspecified,
                Unspecified = unspecified,
                Utc = unspecified,
                Ticks = unspecified,
                String = unspecified,
                DateOnlyString = unspecified.Date,
                Document = unspecified
            };
            var json = obj.ToJson();
            var expected = __expectedTemplate;
            var milliseconds = (long)(utc - BsonConstants.UnixEpoch).TotalMilliseconds;
            var utcJson = string.Format("ISODate(\"{0}\")", utc.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"));
            expected = expected.Replace("#Default", utcJson);
            expected = expected.Replace("#Local", utcJson);
            expected = expected.Replace("#Unspecified", utcJson);
            expected = expected.Replace("#Utc", utcJson);
            expected = expected.Replace("#Ticks", string.Format("NumberLong('{0}')", utc.Ticks.ToString()));
            expected = expected.Replace("#String", unspecified.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz"));
            expected = expected.Replace("#DateOnlyString", unspecified.Date.ToString("yyyy-MM-dd"));
            expected = expected.Replace("#Document", "{ 'DateTime' : #D, 'Ticks' : NumberLong('#T') }".Replace("#D", utcJson).Replace("#T", utc.Ticks.ToString()));
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            var utcTruncated = BsonConstants.UnixEpoch.AddMilliseconds(milliseconds); // loss of precision
            var localTruncated = utcTruncated.ToLocalTime();
            Assert.Equal(utcTruncated, rehydrated.Default);
            Assert.Equal(localTruncated, rehydrated.Local);
            Assert.Equal(localTruncated, rehydrated.Unspecified);
            Assert.Equal(utcTruncated, rehydrated.Utc);
            Assert.Equal(utc, rehydrated.Ticks);
            Assert.Equal(utc, rehydrated.String);
            Assert.Equal(unspecified.Date, rehydrated.DateOnlyString);
            Assert.Equal(utc, rehydrated.Document);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.Equal(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.Equal(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Document.Kind);
        }

        [Fact]
        public void TestUtc()
        {
            var utc = DateTime.UtcNow;
            var obj = new TestClass
            {
                Default = utc,
                Local = utc,
                Unspecified = utc,
                Utc = utc,
                Ticks = utc,
                String = utc,
                DateOnlyString = utc.Date,
                Document = utc
            };
            var json = obj.ToJson();
            var expected = __expectedTemplate;
            var milliseconds = (long)(utc - BsonConstants.UnixEpoch).TotalMilliseconds;
            var utcJson = string.Format("ISODate(\"{0}\")", utc.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"));
            expected = expected.Replace("#Default", utcJson);
            expected = expected.Replace("#Local", utcJson);
            expected = expected.Replace("#Unspecified", utcJson);
            expected = expected.Replace("#Utc", utcJson);
            expected = expected.Replace("#Ticks", string.Format("NumberLong('{0}')", utc.Ticks.ToString()));
            expected = expected.Replace("#String", utc.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFZ"));
            expected = expected.Replace("#DateOnlyString", utc.Date.ToString("yyyy-MM-dd"));
            expected = expected.Replace("#Document", "{ 'DateTime' : #D, 'Ticks' : NumberLong('#T') }".Replace("#D", utcJson).Replace("#T", utc.Ticks.ToString()));
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            var utcTruncated = BsonConstants.UnixEpoch.AddMilliseconds(milliseconds); // loss of precision
            var localTruncated = utcTruncated.ToLocalTime();
            Assert.Equal(utcTruncated, rehydrated.Default);
            Assert.Equal(localTruncated, rehydrated.Local);
            Assert.Equal(localTruncated, rehydrated.Unspecified);
            Assert.Equal(utcTruncated, rehydrated.Utc);
            Assert.Equal(utc, rehydrated.Ticks);
            Assert.Equal(utc, rehydrated.String);
            Assert.Equal(utc.Date, rehydrated.DateOnlyString);
            Assert.Equal(utc, rehydrated.Document);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Default.Kind);
            Assert.Equal(DateTimeKind.Local, rehydrated.Local.Kind);
            Assert.Equal(DateTimeKind.Unspecified, rehydrated.Unspecified.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Utc.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Ticks.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.String.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.DateOnlyString.Kind);
            Assert.Equal(DateTimeKind.Utc, rehydrated.Document.Kind);
        }
    }

    public class DoubleSerializerTests
    {
        public class TestClass
        {
            public double D;
            [BsonRepresentation(BsonType.Int32, AllowTruncation = true)]
            public double I;
            [BsonRepresentation(BsonType.Int64, AllowTruncation = true)]
            public double L;
            [BsonRepresentation(BsonType.String)]
            public double S;
        }

        [Fact]
        public void TestMin()
        {
            var obj = new TestClass
            {
                D = double.MinValue,
                I = 0,
                L = 0,
                S = double.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : #, 'I' : 0, 'L' : NumberLong(0), 'S' : '#' }";
            expected = expected.Replace("#", "-1.7976931348623157E+308");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinusOne()
        {
            var obj = new TestClass
            {
                D = -1.0,
                I = -1.0,
                L = -1.0,
                S = -1.0
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : -1.0, 'I' : -1, 'L' : NumberLong(-1), 'S' : '-1' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestZero()
        {
            var obj = new TestClass
            {
                D = 0.0,
                I = 0.0,
                L = 0.0,
                S = 0.0
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 0.0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOne()
        {
            var obj = new TestClass
            {
                D = 1.0,
                I = 1.0,
                L = 1.0,
                S = 1.0
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 1.0, 'I' : 1, 'L' : NumberLong(1), 'S' : '1' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOnePointFive()
        {
            var obj = new TestClass
            {
                D = 1.5,
                I = 1.5,
                L = 1.5,
                S = 1.5
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : 1.5, 'I' : 1, 'L' : NumberLong(1), 'S' : '1.5' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMax()
        {
            var obj = new TestClass
            {
                D = double.MaxValue,
                I = 0,
                L = 0,
                S = double.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : #, 'I' : 0, 'L' : NumberLong(0), 'S' : '#' }";
            expected = expected.Replace("#", "1.7976931348623157E+308");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNaN()
        {
            var obj = new TestClass
            {
                D = double.NaN,
                I = 0,
                L = 0,
                S = double.NaN
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : #, 'I' : 0, 'L' : NumberLong(0), 'S' : '#' }";
            expected = expected.Replace("#", "NaN");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNegativeInfinity()
        {
            var obj = new TestClass
            {
                D = double.NegativeInfinity,
                I = 0,
                L = 0,
                S = double.NegativeInfinity
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : #, 'I' : 0, 'L' : NumberLong(0), 'S' : '#' }";
            expected = expected.Replace("#", "-Infinity");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestPositiveInfinity()
        {
            var obj = new TestClass
            {
                D = double.PositiveInfinity,
                I = 0,
                L = 0,
                S = double.PositiveInfinity
            };
            var json = obj.ToJson();
            var expected = "{ 'D' : #, 'I' : 0, 'L' : NumberLong(0), 'S' : '#' }";
            expected = expected.Replace("#", "Infinity");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class GuidSerializerTests
    {
        public class TestClass
        {
            public Guid Binary { get; set; }
            [BsonRepresentation(BsonType.String)]
            public Guid String { get; set; }
        }

        [Fact]
        public void TestEmpty()
        {
            var guid = Guid.Empty;
            var obj = new TestClass
            {
                Binary = guid,
                String = guid
            };
            var json = obj.ToJson();
            var expected = "{ 'Binary' : CSUUID('#B'), 'String' : '#S' }";
            expected = expected.Replace("#B", "00000000-0000-0000-0000-000000000000");
            expected = expected.Replace("#S", "00000000-0000-0000-0000-000000000000");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestGuidRepresentation()
        {
            var s = "01020304-0506-0708-090a-0b0c0d0e0f10";
            var guid = new Guid(s);
            var obj = new TestClass
            {
                Binary = guid,
                String = guid
            };
            var json = obj.ToJson();
            var expected = "{ 'Binary' : CSUUID('#B'), 'String' : '#S' }";
            expected = expected.Replace("#B", s);
            expected = expected.Replace("#S", s);
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class Int32SerializerTests
    {
        public class TestClass
        {
            [BsonRepresentation(BsonType.Decimal128)]
            public int D128;
            [BsonRepresentation(BsonType.Double)]
            public int D;
            [BsonRepresentation(BsonType.Int32)]
            public int I;
            [BsonRepresentation(BsonType.Int64)]
            public int L;
            [BsonRepresentation(BsonType.String)]
            public int S;
        }

        [Fact]
        public void TestMin()
        {
            var obj = new TestClass
            {
                D128 = int.MinValue,
                D = int.MinValue,
                I = int.MinValue,
                L = int.MinValue,
                S = int.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'D128' : NumberDecimal('#'), 'D' : #.0, 'I' : #, 'L' : NumberLong(#), 'S' : '#' }";
            expected = expected.Replace("#", int.MinValue.ToString());
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinusOne()
        {
            var obj = new TestClass
            {
                D128 = -1,
                D = -1,
                I = -1,
                L = -1,
                S = -1
            };
            var json = obj.ToJson();
            var expected = "{ 'D128' : NumberDecimal('-1'), 'D' : -1.0, 'I' : -1, 'L' : NumberLong(-1), 'S' : '-1' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestZero()
        {
            var obj = new TestClass
            {
                D128 = 0,
                D = 0,
                I = 0,
                L = 0,
                S = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'D128' : NumberDecimal('0'), 'D' : 0.0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOne()
        {
            var obj = new TestClass
            {
                D128 = 1,
                D = 1,
                I = 1,
                L = 1,
                S = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'D128' : NumberDecimal('1'), 'D' : 1.0, 'I' : 1, 'L' : NumberLong(1), 'S' : '1' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMax()
        {
            var obj = new TestClass
            {
                D128 = int.MaxValue,
                D = int.MaxValue,
                I = int.MaxValue,
                L = int.MaxValue,
                S = int.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'D128' : NumberDecimal('#'), 'D' : #.0, 'I' : #, 'L' : NumberLong(#), 'S' : '#' }";
            expected = expected.Replace("#", int.MaxValue.ToString());
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class Int64SerializerTests
    {
        public class TestClass
        {
            [BsonRepresentation(BsonType.Decimal128)]
            public long D128;
            [BsonRepresentation(BsonType.Double)]
            public long D;
            [BsonRepresentation(BsonType.Int32)]
            public long I;
            [BsonRepresentation(BsonType.Int64)]
            public long L;
            [BsonRepresentation(BsonType.String)]
            public long S;
        }

        [Fact]
        public void TestMin()
        {
            var obj = new TestClass
            {
                D128 = long.MinValue,
                D = 0,
                I = 0,
                L = long.MinValue,
                S = long.MinValue
            };
            var json = obj.ToJson();
            var expected = "{ 'D128' : NumberDecimal('#'), 'D' : 0.0, 'I' : 0, 'L' : NumberLong('#'), 'S' : '#' }";
            expected = expected.Replace("#", long.MinValue.ToString());
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinusOne()
        {
            var obj = new TestClass
            {
                D128 = -1,
                D = -1,
                I = -1,
                L = -1,
                S = -1
            };
            var json = obj.ToJson();
            var expected = "{ 'D128' : NumberDecimal('-1'), 'D' : -1.0, 'I' : -1, 'L' : NumberLong(-1), 'S' : '-1' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestZero()
        {
            var obj = new TestClass
            {
                D128 = 0,
                D = 0,
                I = 0,
                L = 0,
                S = 0
            };
            var json = obj.ToJson();
            var expected = "{ 'D128' : NumberDecimal('0'), 'D' : 0.0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestOne()
        {
            var obj = new TestClass
            {
                D128 = 1,
                D = 1,
                I = 1,
                L = 1,
                S = 1
            };
            var json = obj.ToJson();
            var expected = "{ 'D128' : NumberDecimal('1'), 'D' : 1.0, 'I' : 1, 'L' : NumberLong(1), 'S' : '1' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMax()
        {
            var obj = new TestClass
            {
                D128 = long.MaxValue,
                D = 0,
                I = 0,
                L = long.MaxValue,
                S = long.MaxValue
            };
            var json = obj.ToJson();
            var expected = "{ 'D128' : NumberDecimal('#'), 'D' : 0.0, 'I' : 0, 'L' : NumberLong('#'), 'S' : '#' }";
            expected = expected.Replace("#", long.MaxValue.ToString());
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class ObjectIdSerializerTests
    {
        public class TestClass
        {
            public ObjectId ObjectId { get; set; }
            [BsonRepresentation(BsonType.String)]
            public ObjectId String { get; set; }
        }

        [Fact]
        public void TestSerializer()
        {
            var objectId = new ObjectId(1, 2, 3, 4);
            var obj = new TestClass
            {
                ObjectId = objectId,
                String = objectId
            };
            var json = obj.ToJson();
            var expected = ("{ 'ObjectId' : #O, 'String' : #S }");
            expected = expected.Replace("#O", "ObjectId('000000010000020003000004')");
            expected = expected.Replace("#S", "'000000010000020003000004'");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class StringSerializerTests
    {
        public class TestClass
        {
            public String String { get; set; }
        }

        [Fact]
        public void TestNull()
        {
            var obj = new TestClass
            {
                String = null
            };
            var json = obj.ToJson();
            var expected = ("{ 'String' : null }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEmpty()
        {
            var obj = new TestClass
            {
                String = String.Empty
            };
            var json = obj.ToJson();
            var expected = ("{ 'String' : '' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestHelloWorld()
        {
            var obj = new TestClass
            {
                String = "Hello World"
            };
            var json = obj.ToJson();
            var expected = ("{ 'String' : 'Hello World' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClass>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
