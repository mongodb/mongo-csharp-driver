﻿/* Copyright 2010-present MongoDB Inc.
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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class EnumSerializerByteTests
    {
        private enum E : byte
        {
            A = 1,
            B = 2
        }

        private class C
        {
            public E D { get; set; }
            [BsonRepresentation(BsonType.Int32)]
            public E I { get; set; }
            [BsonRepresentation(BsonType.Int64)]
            public E L { get; set; }
            [BsonRepresentation(BsonType.String)]
            public E S { get; set; }
        }

        [Fact]
        public void TestSerializeZero()
        {
            C c = new C { D = 0, I = 0, L = 0, S = 0 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));

            var document = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(BsonType.Int32, document["D"].BsonType);
            Assert.Equal(BsonType.Int32, document["I"].BsonType);
            Assert.Equal(BsonType.Int64, document["L"].BsonType);
            Assert.Equal(BsonType.String, document["S"].BsonType);
        }

        [Fact]
        public void TestSerializeA()
        {
            C c = new C { D = E.A, I = E.A, L = E.A, S = E.A };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 1, 'I' : 1, 'L' : NumberLong(1), 'S' : 'A' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeB()
        {
            C c = new C { D = E.B, I = E.B, L = E.B, S = E.B };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 2, 'I' : 2, 'L' : NumberLong(2), 'S' : 'B' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeInvalid()
        {
            C c = new C { D = (E)123, I = (E)123, L = (E)123, S = (E)123 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 123, 'I' : 123, 'L' : NumberLong(123), 'S' : '123' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDeserializeDouble()
        {
            var document = new BsonDocument
            {
                { "D", 1.0 },
                { "I", 1.0 },
                { "L", 1.0 },
                { "S", 1.0 }
            };
            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.Equal(E.A, rehydrated.D);
            Assert.Equal(E.A, rehydrated.I);
            Assert.Equal(E.A, rehydrated.L);
            Assert.Equal(E.A, rehydrated.S);
        }
    }

    public class EnumSerializerInt16Tests
    {
        private enum E : short
        {
            A = 1,
            B = 2
        }

        private class C
        {
            public E D { get; set; }
            [BsonRepresentation(BsonType.Int32)]
            public E I { get; set; }
            [BsonRepresentation(BsonType.Int64)]
            public E L { get; set; }
            [BsonRepresentation(BsonType.String)]
            public E S { get; set; }
        }

        [Fact]
        public void TestSerializeZero()
        {
            C c = new C { D = 0, I = 0, L = 0, S = 0 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));

            var document = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(BsonType.Int32, document["D"].BsonType);
            Assert.Equal(BsonType.Int32, document["I"].BsonType);
            Assert.Equal(BsonType.Int64, document["L"].BsonType);
            Assert.Equal(BsonType.String, document["S"].BsonType);
        }

        [Fact]
        public void TestSerializeA()
        {
            C c = new C { D = E.A, I = E.A, L = E.A, S = E.A };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 1, 'I' : 1, 'L' : NumberLong(1), 'S' : 'A' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeB()
        {
            C c = new C { D = E.B, I = E.B, L = E.B, S = E.B };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 2, 'I' : 2, 'L' : NumberLong(2), 'S' : 'B' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeInvalid()
        {
            C c = new C { D = (E)123, I = (E)123, L = (E)123, S = (E)123 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 123, 'I' : 123, 'L' : NumberLong(123), 'S' : '123' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDeserializeDouble()
        {
            var document = new BsonDocument
            {
                { "D", 1.0 },
                { "I", 1.0 },
                { "L", 1.0 },
                { "S", 1.0 }
            };
            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.Equal(E.A, rehydrated.D);
            Assert.Equal(E.A, rehydrated.I);
            Assert.Equal(E.A, rehydrated.L);
            Assert.Equal(E.A, rehydrated.S);
        }
    }

    public class EnumSerializerInt32Tests
    {
        private enum E : int
        {
            A = 1,
            B = 2
        }

        private class C
        {
            public E D { get; set; }
            [BsonRepresentation(BsonType.Int32)]
            public E I { get; set; }
            [BsonRepresentation(BsonType.Int64)]
            public E L { get; set; }
            [BsonRepresentation(BsonType.String)]
            public E S { get; set; }
        }

        [Fact]
        public void TestSerializeZero()
        {
            C c = new C { D = 0, I = 0, L = 0, S = 0 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));

            var document = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(BsonType.Int32, document["D"].BsonType);
            Assert.Equal(BsonType.Int32, document["I"].BsonType);
            Assert.Equal(BsonType.Int64, document["L"].BsonType);
            Assert.Equal(BsonType.String, document["S"].BsonType);
        }

        [Fact]
        public void TestSerializeA()
        {
            C c = new C { D = E.A, I = E.A, L = E.A, S = E.A };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 1, 'I' : 1, 'L' : NumberLong(1), 'S' : 'A' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeB()
        {
            C c = new C { D = E.B, I = E.B, L = E.B, S = E.B };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 2, 'I' : 2, 'L' : NumberLong(2), 'S' : 'B' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeInvalid()
        {
            C c = new C { D = (E)123, I = (E)123, L = (E)123, S = (E)123 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 123, 'I' : 123, 'L' : NumberLong(123), 'S' : '123' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDeserializeDouble()
        {
            var document = new BsonDocument
            {
                { "D", 1.0 },
                { "I", 1.0 },
                { "L", 1.0 },
                { "S", 1.0 }
            };
            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.Equal(E.A, rehydrated.D);
            Assert.Equal(E.A, rehydrated.I);
            Assert.Equal(E.A, rehydrated.L);
            Assert.Equal(E.A, rehydrated.S);
        }
    }

    public class EnumSerializerInt64Tests
    {
        private enum E : long
        {
            A = 1,
            B = 2
        }

        private class C
        {
            public E D { get; set; }
            [BsonRepresentation(BsonType.Int32)]
            public E I { get; set; }
            [BsonRepresentation(BsonType.Int64)]
            public E L { get; set; }
            [BsonRepresentation(BsonType.String)]
            public E S { get; set; }
        }

        [Fact]
        public void TestSerializeZero()
        {
            C c = new C { D = 0, I = 0, L = 0, S = 0 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : NumberLong(0), 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));

            var document = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(BsonType.Int64, document["D"].BsonType);
            Assert.Equal(BsonType.Int32, document["I"].BsonType);
            Assert.Equal(BsonType.Int64, document["L"].BsonType);
            Assert.Equal(BsonType.String, document["S"].BsonType);
        }

        [Fact]
        public void TestSerializeA()
        {
            C c = new C { D = E.A, I = E.A, L = E.A, S = E.A };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : NumberLong(1), 'I' : 1, 'L' : NumberLong(1), 'S' : 'A' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeB()
        {
            C c = new C { D = E.B, I = E.B, L = E.B, S = E.B };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : NumberLong(2), 'I' : 2, 'L' : NumberLong(2), 'S' : 'B' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeInvalid()
        {
            C c = new C { D = (E)123, I = (E)123, L = (E)123, S = (E)123 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : NumberLong(123), 'I' : 123, 'L' : NumberLong(123), 'S' : '123' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDeserializeDouble()
        {
            var document = new BsonDocument
            {
                { "D", 1.0 },
                { "I", 1.0 },
                { "L", 1.0 },
                { "S", 1.0 }
            };
            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.Equal(E.A, rehydrated.D);
            Assert.Equal(E.A, rehydrated.I);
            Assert.Equal(E.A, rehydrated.L);
            Assert.Equal(E.A, rehydrated.S);
        }
    }

    public class EnumSerializerSByteTests
    {
        private enum E : sbyte
        {
            A = 1,
            B = 2
        }

        private class C
        {
            public E D { get; set; }
            [BsonRepresentation(BsonType.Int32)]
            public E I { get; set; }
            [BsonRepresentation(BsonType.Int64)]
            public E L { get; set; }
            [BsonRepresentation(BsonType.String)]
            public E S { get; set; }
        }

        [Fact]
        public void TestSerializeZero()
        {
            C c = new C { D = 0, I = 0, L = 0, S = 0 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));

            var document = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(BsonType.Int32, document["D"].BsonType);
            Assert.Equal(BsonType.Int32, document["I"].BsonType);
            Assert.Equal(BsonType.Int64, document["L"].BsonType);
            Assert.Equal(BsonType.String, document["S"].BsonType);
        }

        [Fact]
        public void TestSerializeA()
        {
            C c = new C { D = E.A, I = E.A, L = E.A, S = E.A };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 1, 'I' : 1, 'L' : NumberLong(1), 'S' : 'A' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeB()
        {
            C c = new C { D = E.B, I = E.B, L = E.B, S = E.B };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 2, 'I' : 2, 'L' : NumberLong(2), 'S' : 'B' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeInvalid()
        {
            C c = new C { D = (E)123, I = (E)123, L = (E)123, S = (E)123 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 123, 'I' : 123, 'L' : NumberLong(123), 'S' : '123' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDeserializeDouble()
        {
            var document = new BsonDocument
            {
                { "D", 1.0 },
                { "I", 1.0 },
                { "L", 1.0 },
                { "S", 1.0 }
            };
            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.Equal(E.A, rehydrated.D);
            Assert.Equal(E.A, rehydrated.I);
            Assert.Equal(E.A, rehydrated.L);
            Assert.Equal(E.A, rehydrated.S);
        }
    }

    public class EnumSerializerUInt16Tests
    {
        private enum E : ushort
        {
            A = 1,
            B = 2
        }

        private class C
        {
            public E D { get; set; }
            [BsonRepresentation(BsonType.Int32)]
            public E I { get; set; }
            [BsonRepresentation(BsonType.Int64)]
            public E L { get; set; }
            [BsonRepresentation(BsonType.String)]
            public E S { get; set; }
        }

        [Fact]
        public void TestSerializeZero()
        {
            C c = new C { D = 0, I = 0, L = 0, S = 0 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));

            var document = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(BsonType.Int32, document["D"].BsonType);
            Assert.Equal(BsonType.Int32, document["I"].BsonType);
            Assert.Equal(BsonType.Int64, document["L"].BsonType);
            Assert.Equal(BsonType.String, document["S"].BsonType);
        }

        [Fact]
        public void TestSerializeA()
        {
            C c = new C { D = E.A, I = E.A, L = E.A, S = E.A };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 1, 'I' : 1, 'L' : NumberLong(1), 'S' : 'A' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeB()
        {
            C c = new C { D = E.B, I = E.B, L = E.B, S = E.B };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 2, 'I' : 2, 'L' : NumberLong(2), 'S' : 'B' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeInvalid()
        {
            C c = new C { D = (E)123, I = (E)123, L = (E)123, S = (E)123 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 123, 'I' : 123, 'L' : NumberLong(123), 'S' : '123' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDeserializeDouble()
        {
            var document = new BsonDocument
            {
                { "D", 1.0 },
                { "I", 1.0 },
                { "L", 1.0 },
                { "S", 1.0 }
            };
            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.Equal(E.A, rehydrated.D);
            Assert.Equal(E.A, rehydrated.I);
            Assert.Equal(E.A, rehydrated.L);
            Assert.Equal(E.A, rehydrated.S);
        }
    }

    public class EnumSerializerUInt32Tests
    {
        private enum E : uint
        {
            A = 1,
            B = 2
        }

        private class C
        {
            public E D { get; set; }
            [BsonRepresentation(BsonType.Int32)]
            public E I { get; set; }
            [BsonRepresentation(BsonType.Int64)]
            public E L { get; set; }
            [BsonRepresentation(BsonType.String)]
            public E S { get; set; }
        }

        [Fact]
        public void TestSerializeZero()
        {
            C c = new C { D = 0, I = 0, L = 0, S = 0 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 0, 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));

            var document = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(BsonType.Int32, document["D"].BsonType);
            Assert.Equal(BsonType.Int32, document["I"].BsonType);
            Assert.Equal(BsonType.Int64, document["L"].BsonType);
            Assert.Equal(BsonType.String, document["S"].BsonType);
        }

        [Fact]
        public void TestSerializeA()
        {
            C c = new C { D = E.A, I = E.A, L = E.A, S = E.A };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 1, 'I' : 1, 'L' : NumberLong(1), 'S' : 'A' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeB()
        {
            C c = new C { D = E.B, I = E.B, L = E.B, S = E.B };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 2, 'I' : 2, 'L' : NumberLong(2), 'S' : 'B' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeInvalid()
        {
            C c = new C { D = (E)123, I = (E)123, L = (E)123, S = (E)123 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : 123, 'I' : 123, 'L' : NumberLong(123), 'S' : '123' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDeserializeDouble()
        {
            var document = new BsonDocument
            {
                { "D", 1.0 },
                { "I", 1.0 },
                { "L", 1.0 },
                { "S", 1.0 }
            };
            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.Equal(E.A, rehydrated.D);
            Assert.Equal(E.A, rehydrated.I);
            Assert.Equal(E.A, rehydrated.L);
            Assert.Equal(E.A, rehydrated.S);
        }
    }

    public class EnumSerializerUInt64Tests
    {
        private enum E : ulong
        {
            A = 1,
            B = 2
        }

        private class C
        {
            public E D { get; set; }
            [BsonRepresentation(BsonType.Int32)]
            public E I { get; set; }
            [BsonRepresentation(BsonType.Int64)]
            public E L { get; set; }
            [BsonRepresentation(BsonType.String)]
            public E S { get; set; }
        }

        [Fact]
        public void TestSerializeZero()
        {
            C c = new C { D = 0, I = 0, L = 0, S = 0 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : NumberLong(0), 'I' : 0, 'L' : NumberLong(0), 'S' : '0' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));

            var document = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(BsonType.Int64, document["D"].BsonType);
            Assert.Equal(BsonType.Int32, document["I"].BsonType);
            Assert.Equal(BsonType.Int64, document["L"].BsonType);
            Assert.Equal(BsonType.String, document["S"].BsonType);
        }

        [Fact]
        public void TestSerializeA()
        {
            C c = new C { D = E.A, I = E.A, L = E.A, S = E.A };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : NumberLong(1), 'I' : 1, 'L' : NumberLong(1), 'S' : 'A' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeB()
        {
            C c = new C { D = E.B, I = E.B, L = E.B, S = E.B };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : NumberLong(2), 'I' : 2, 'L' : NumberLong(2), 'S' : 'B' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeInvalid()
        {
            C c = new C { D = (E)123, I = (E)123, L = (E)123, S = (E)123 };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = ("{ 'D' : NumberLong(123), 'I' : 123, 'L' : NumberLong(123), 'S' : '123' }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDeserializeDouble()
        {
            var document = new BsonDocument
            {
                { "D", 1.0 },
                { "I", 1.0 },
                { "L", 1.0 },
                { "S", 1.0 }
            };
            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.Equal(E.A, rehydrated.D);
            Assert.Equal(E.A, rehydrated.I);
            Assert.Equal(E.A, rehydrated.L);
            Assert.Equal(E.A, rehydrated.S);
        }
    }
}
