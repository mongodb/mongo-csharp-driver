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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class RawBsonDocumentSerializerTests
    {
        public class C : IDisposable
        {
            public RawBsonDocument D;

            public void Dispose()
            {
                if (D != null)
                {
                    D.Dispose();
                    D = null;
                }
            }
        }

        [Fact]
        public void TestRoundTrip()
        {
            var bsonDocument = new BsonDocument { { "D", new BsonDocument { { "x", 1 }, { "y", 2 } } } };
            var bson = bsonDocument.ToBson();
            var json = bsonDocument.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });

            using (var c = BsonSerializer.Deserialize<C>(bson))
            {
                Assert.True(bson.SequenceEqual(c.ToBson()));
            }

            using (var c = BsonSerializer.Deserialize<C>(json))
            {
                Assert.True(bson.SequenceEqual(c.ToBson()));
            }

            using (var c = BsonSerializer.Deserialize<C>(json))
            {
                Assert.Equal(json, c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell }));
            }
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new RawBsonDocumentSerializer();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new RawBsonDocumentSerializer();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new RawBsonDocumentSerializer();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new RawBsonDocumentSerializer();
            var y = new RawBsonDocumentSerializer();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new RawBsonDocumentSerializer();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }
}
