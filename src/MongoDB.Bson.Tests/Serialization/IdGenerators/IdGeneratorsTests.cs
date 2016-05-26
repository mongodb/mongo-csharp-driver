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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class TestIdGenerators
    {
        private struct S : IEquatable<S>
        {
            public int I;
            public bool Equals(S other)
            {
                return this.I == other.I;
            }
        };

        [Fact]
        public void TestGuidIdChecker()
        {
            var idChecker = BsonSerializer.LookupIdGenerator(typeof(Guid));
            Assert.True(idChecker.IsEmpty(Guid.Empty));
            Assert.False(idChecker.IsEmpty(Guid.NewGuid()));
        }

        [Fact]
        public void TestIntZeroIdChecker()
        {
            var idChecker = new ZeroIdChecker<int>();
            Assert.True(idChecker.IsEmpty(0));
            Assert.False(idChecker.IsEmpty(1));
        }

        [Fact]
        public void TestNullIdChecker()
        {
            var idChecker = new NullIdChecker();
            Assert.True(idChecker.IsEmpty(null));
            Assert.False(idChecker.IsEmpty(new object()));
        }

        [Fact]
        public void TestObjectIdChecker()
        {
            var idChecker = BsonSerializer.LookupIdGenerator(typeof(ObjectId));
            Assert.True(idChecker.IsEmpty(ObjectId.Empty));
            Assert.False(idChecker.IsEmpty(ObjectId.GenerateNewId()));
        }

        [Fact]
        public void TestStructZeroIdChecker()
        {
            var idChecker = new ZeroIdChecker<S>();
            Assert.True(idChecker.IsEmpty(default(S)));
            Assert.True(idChecker.IsEmpty(new S()));
            Assert.True(idChecker.IsEmpty(new S { I = 0 }));
            Assert.False(idChecker.IsEmpty(new S { I = 1 }));
        }
    }
}
