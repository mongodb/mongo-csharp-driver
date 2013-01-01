/* Copyright 2010-2013 10gen Inc.
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
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp452Tests
    {
        public class A
        {
            private static readonly DateTime __staticTime = DateTime.UtcNow;

            [BsonElement("a", Order = 0)]
            [BsonIgnoreIfDefault]
            public DateTime DateTime1
            {
                get;
                set;
            }

            [BsonElement("b", Order = 1)]
            [BsonIgnoreIfDefault]
            public DateTime DateTime2
            {
                get
                {
                    return __staticTime;
                }
            }

            [BsonElement("c", Order = 2)]
            [BsonIgnoreIfDefault]
            public DateTime DateTime3
            {
                get
                {
                    return DateTime.MinValue;
                }
            }
        }

        [Test]
        public void TestReadonlyMembers()
        {
            var document = new A
            {
                DateTime1 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
