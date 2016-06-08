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
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class LazyBsonDocumentSerializerTests
    {
        public class C : IDisposable
        {
            public LazyBsonDocument D;

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

            using (var c = BsonSerializer.Deserialize<C>(bson))
            {
                Assert.True(bson.SequenceEqual(c.ToBson()));
            }
        }
    }
}
