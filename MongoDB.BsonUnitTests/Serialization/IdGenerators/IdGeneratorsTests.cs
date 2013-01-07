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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
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

        [Test]
        public void TestGuidIdChecker()
        {
            var idChecker = BsonSerializer.LookupIdGenerator(typeof(Guid));
            Assert.IsTrue(idChecker.IsEmpty(Guid.Empty));
            Assert.IsFalse(idChecker.IsEmpty(Guid.NewGuid()));
        }

        [Test]
        public void TestIntZeroIdChecker()
        {
            var idChecker = new ZeroIdChecker<int>();
            Assert.IsTrue(idChecker.IsEmpty(0));
            Assert.IsFalse(idChecker.IsEmpty(1));
        }

        [Test]
        public void TestNullIdChecker()
        {
            var idChecker = new NullIdChecker();
            Assert.IsTrue(idChecker.IsEmpty(null));
            Assert.IsFalse(idChecker.IsEmpty(new object()));
        }

        [Test]
        public void TestObjectIdChecker()
        {
            var idChecker = BsonSerializer.LookupIdGenerator(typeof(ObjectId));
            Assert.IsTrue(idChecker.IsEmpty(ObjectId.Empty));
            Assert.IsFalse(idChecker.IsEmpty(ObjectId.GenerateNewId()));
        }

        [Test]
        public void TestStructZeroIdChecker()
        {
            var idChecker = new ZeroIdChecker<S>();
            Assert.IsTrue(idChecker.IsEmpty(default(S)));
            Assert.IsTrue(idChecker.IsEmpty(new S()));
            Assert.IsTrue(idChecker.IsEmpty(new S { I = 0 }));
            Assert.IsFalse(idChecker.IsEmpty(new S { I = 1 }));
        }
    }
}
