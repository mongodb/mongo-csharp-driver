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
using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp2127Tests
    {
        public enum E : uint
        {
            IntMaxValuePlusOne = (uint)int.MaxValue + 1,
            UIntMaxValue = uint.MaxValue
        }

        private class TestClassWithAllowOverflowTrue
        {
            [BsonRepresentation(BsonType.EndOfDocument, AllowOverflow = true)]
            public E D { get; set; }
            [BsonRepresentation(BsonType.Int32, AllowOverflow = true)]
            public E I { get; set; }
            [BsonRepresentation(BsonType.Int64)]
            public E L { get; set; }
            [BsonRepresentation(BsonType.String)]
            public E S { get; set; }
        }

        private class TestClassWithAllowOverflowFalse
        {
            [BsonRepresentation(BsonType.EndOfDocument, AllowOverflow = false)]
            public E D { get; set; }
            [BsonRepresentation(BsonType.Int32, AllowOverflow = false)]
            public E I { get; set; }
            [BsonRepresentation(BsonType.Int64)]
            public E L { get; set; }
            [BsonRepresentation(BsonType.String)]
            public E S { get; set; }
        }

        [Theory]
        [InlineData(E.IntMaxValuePlusOne)]
        [InlineData(E.UIntMaxValue)]
        public void TestSerializeWithOverflowAttributeTrue(E e)
        {
            var c = new TestClassWithAllowOverflowTrue { D = e, I = e, L = e, S = e };
            var json = c.ToJson();
            var expected = $"{{ 'D' : {(int)e}, 'I' : {(int)e}, 'L' : {ToNumberLongString((long)e)}, 'S' : '{e}' }}".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClassWithAllowOverflowTrue>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));

            var document = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(BsonType.Int32, document["D"].BsonType);
            Assert.Equal(BsonType.Int32, document["I"].BsonType);
            Assert.Equal(BsonType.Int64, document["L"].BsonType);
            Assert.Equal(BsonType.String, document["S"].BsonType);
        }

        [Theory]
        [InlineData(E.IntMaxValuePlusOne)]
        [InlineData(E.UIntMaxValue)]
        public void TestSerializeWithOverflowAttributeFalse(E e)
        {
            var c = new TestClassWithAllowOverflowFalse { D = e, I = e, L = e, S = e };
            Assert.Throws<OverflowException>(() => c.ToJson());
            Assert.Throws<OverflowException>(() => c.ToBson());
        }

        [Theory]
        [InlineData(E.IntMaxValuePlusOne)]
        [InlineData(E.UIntMaxValue)]
        public void TestDeserializeDoubleWithAllowOverflowTrue(E e)
        {
            var document = new BsonDocument
            {
                { "D", (double)e },
                { "I", (double)e },
                { "L", (double)e },
                { "S", (double)e }
            };
            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClassWithAllowOverflowTrue>(bson);
            Assert.Equal(e, rehydrated.D);
            Assert.Equal(e, rehydrated.I);
            Assert.Equal(e, rehydrated.L);
            Assert.Equal(e, rehydrated.S);
        }

        [Theory]
        [InlineData(E.IntMaxValuePlusOne)]
        [InlineData(E.UIntMaxValue)]
        public void TestDeserializeDoubleWithAllowOverflowFalse(E e)
        {
            var document = new BsonDocument
            {
                { "D", (double)e },
                { "I", (double)e },
                { "L", (double)e },
                { "S", (double)e }
            };
            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<TestClassWithAllowOverflowFalse>(bson);
            Assert.Equal(e, rehydrated.D);
            Assert.Equal(e, rehydrated.I);
            Assert.Equal(e, rehydrated.L);
            Assert.Equal(e, rehydrated.S);
        }

        /// <summary>
        /// Mimics the behaviour of <see cref="Bson.IO.JsonWriter.WriteInt64"/> by wrapping values above/below the Int32 boundaries in quotes
        /// </summary>
        private static string ToNumberLongString(long enumValueAsInt64)
        {
            return string.Format("NumberLong({1}{0}{1})", enumValueAsInt64, enumValueAsInt64 >= int.MinValue && enumValueAsInt64 <= int.MaxValue ? string.Empty : "\"");
        }
    }
}
