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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions.DateTimeSerializationOptions
{
    public class LocalTests
    {
        public class C
        {
            [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
            public DateTime DT { get; set; }
            [BsonDateTimeOptions(Kind = DateTimeKind.Local, DateOnly = true)]
            public DateTime D { get; set; }
        }

        [Fact]
        public void TestMaxLocal()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('9999-12-31T23:59:59.999Z'), 'D' : ISODate('9999-12-31T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMaxUnspecified()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Unspecified) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('9999-12-31T23:59:59.999Z'), 'D' : ISODate('9999-12-31T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMaxUtc()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('9999-12-31T23:59:59.999Z'), 'D' : ISODate('9999-12-31T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinLocal()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('0001-01-01T00:00:00Z'), 'D' : ISODate('0001-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinUnspecified()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('0001-01-01T00:00:00Z'), 'D' : ISODate('0001-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinUtc()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('0001-01-01T00:00:00Z'), 'D' : ISODate('0001-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestUnixEpoch()
        {
            var c = new C { DT = BsonConstants.UnixEpoch };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('1970-01-01T00:00:00Z'), 'D' : ISODate('1970-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNow()
        {
            var c = new C { DT = DateTime.Now };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var rep1 = c.DT.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var rep2 = c.D.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var expected = "{ 'DT' : ISODate('#1'), 'D' : ISODate('#2') }".Replace("#1", rep1).Replace("#2", rep2).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            rehydrated.DT = rehydrated.DT.ToLocalTime();
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestUtcNow()
        {
            var c = new C { DT = DateTime.UtcNow };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var rep1 = c.DT.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var rep2 = c.D.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var expected = "{ 'DT' : ISODate('#1'), 'D' : ISODate('#2') }".Replace("#1", rep1).Replace("#2", rep2).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class StringRepresentationTests
    {
        public class C
        {
            [BsonDateTimeOptions(Representation = BsonType.String)]
            public DateTime DT { get; set; }
            [BsonDateTimeOptions(Representation = BsonType.String, DateOnly = true)]
            public DateTime D { get; set; }
        }

        [Fact]
        public void TestMaxLocal()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : '9999-12-31T23:59:59.9999999', 'D' : '9999-12-31' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMaxUnspecified()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Unspecified) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : '9999-12-31T23:59:59.9999999', 'D' : '9999-12-31' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMaxUtc()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : '9999-12-31T23:59:59.9999999', 'D' : '9999-12-31' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinLocal()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : '0001-01-01T00:00:00', 'D' : '0001-01-01' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinUnspecified()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : '0001-01-01T00:00:00', 'D' : '0001-01-01' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinUtc()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : '0001-01-01T00:00:00', 'D' : '0001-01-01' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestUnixEpoch()
        {
            var c = new C { DT = BsonConstants.UnixEpoch };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : '1970-01-01T00:00:00Z', 'D' : '1970-01-01' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNow()
        {
            var c = new C { DT = DateTime.Now };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var rep1 = JsonConvert.ToString(c.DT);
            var rep2 = c.D.ToString("yyyy-MM-dd");
            var expected = "{ 'DT' : '#1', 'D' : '#2' }".Replace("#1", rep1).Replace("#2", rep2).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            rehydrated.DT = rehydrated.DT.ToLocalTime();
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestUtcNow()
        {
            var c = new C { DT = DateTime.UtcNow };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var rep1 = JsonConvert.ToString(c.DT);
            var rep2 = c.D.ToString("yyyy-MM-dd");
            var expected = "{ 'DT' : '#1', 'D' : '#2' }".Replace("#1", rep1).Replace("#2", rep2).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class UnspecifiedTests
    {
        public class C
        {
            [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified)]
            public DateTime DT { get; set; }
            [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified, DateOnly = true)]
            public DateTime D { get; set; }
        }

        [Fact]
        public void TestMaxLocal()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('9999-12-31T23:59:59.999Z'), 'D' : ISODate('9999-12-31T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMaxUnspecified()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Unspecified) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('9999-12-31T23:59:59.999Z'), 'D' : ISODate('9999-12-31T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMaxUtc()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('9999-12-31T23:59:59.999Z'), 'D' : ISODate('9999-12-31T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinLocal()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('0001-01-01T00:00:00Z'), 'D' : ISODate('0001-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinUnspecified()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('0001-01-01T00:00:00Z'), 'D' : ISODate('0001-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinUtc()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('0001-01-01T00:00:00Z'), 'D' : ISODate('0001-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestUnixEpoch()
        {
            var c = new C { DT = BsonConstants.UnixEpoch };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('1970-01-01T00:00:00Z'), 'D' : ISODate('1970-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNow()
        {
            var c = new C { DT = DateTime.Now };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var rep1 = c.DT.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var rep2 = c.D.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var expected = "{ 'DT' : ISODate('#1'), 'D' : ISODate('#2') }".Replace("#1", rep1).Replace("#2", rep2).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestUtcNow()
        {
            var c = new C { DT = DateTime.UtcNow };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var rep1 = c.DT.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var rep2 = c.D.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var expected = "{ 'DT' : ISODate('#1'), 'D' : ISODate('#2') }".Replace("#1", rep1).Replace("#2", rep2).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class UtcTests
    {
        public class C
        {
            [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
            public DateTime DT { get; set; }
            [BsonDateTimeOptions(Kind = DateTimeKind.Utc, DateOnly = true)]
            public DateTime D { get; set; }
        }

        [Fact]
        public void TestMaxLocal()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('9999-12-31T23:59:59.999Z'), 'D' : ISODate('9999-12-31T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMaxUnspecified()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Unspecified) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('9999-12-31T23:59:59.999Z'), 'D' : ISODate('9999-12-31T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMaxUtc()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('9999-12-31T23:59:59.999Z'), 'D' : ISODate('9999-12-31T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinLocal()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('0001-01-01T00:00:00Z'), 'D' : ISODate('0001-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinUnspecified()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('0001-01-01T00:00:00Z'), 'D' : ISODate('0001-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestMinUtc()
        {
            var c = new C { DT = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc) };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('0001-01-01T00:00:00Z'), 'D' : ISODate('0001-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestUnixEpoch()
        {
            var c = new C { DT = BsonConstants.UnixEpoch };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var expected = "{ 'DT' : ISODate('1970-01-01T00:00:00Z'), 'D' : ISODate('1970-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNow()
        {
            var c = new C { DT = DateTime.Now };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var rep1 = c.DT.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var rep2 = c.D.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var expected = "{ 'DT' : ISODate('#1'), 'D' : ISODate('#2') }".Replace("#1", rep1).Replace("#2", rep2).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            rehydrated.DT = rehydrated.DT.ToLocalTime();
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestUtcNow()
        {
            var c = new C { DT = DateTime.UtcNow };
            c.D = c.DT.Date;

            var json = c.ToJson();
            var rep1 = c.DT.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var rep2 = c.D.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
            var expected = "{ 'DT' : ISODate('#1'), 'D' : ISODate('#2') }".Replace("#1", rep1).Replace("#2", rep2).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
