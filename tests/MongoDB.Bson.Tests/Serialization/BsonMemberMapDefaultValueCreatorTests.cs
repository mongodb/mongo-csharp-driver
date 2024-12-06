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

using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonMemberMapDefaultValueCreatorTests
    {
        static BsonMemberMapDefaultValueCreatorTests()
        {
            BsonClassMap.RegisterClassMap<RegisteredWithLambda>(cm =>
            {
                cm.AutoMap();
                cm.GetMemberMap(c => c.L).SetDefaultValue(() => new List<int>());
                cm.GetMemberMap(c => c.Field).SetDefaultValue(() => null);
            });
        }

        [Fact]
        public void TestEachDefaultValueIsNewInstance()
        {
            var json = "{ _id : 1 }";
            var c = BsonSerializer.Deserialize<RegisteredWithLambda>(json);
            var d = BsonSerializer.Deserialize<RegisteredWithLambda>(json);
            Assert.NotNull(c.L);
            Assert.NotNull(d.L);
            Assert.NotSame(c.L, d.L);
        }

        [Fact]
        public void TestModifyingEmptyListDoesNotCorruptDefaultValue()
        {
            var json = "{ _id : 1 }";
            var c1 = BsonSerializer.Deserialize<RegisteredWithLambda>(json);
            c1.L.Add(1);
            var c2 = BsonSerializer.Deserialize<RegisteredWithLambda>(json);
            Assert.Equal(1, c1.L.Count);
            Assert.Equal(0, c2.L.Count);
        }

        [Fact]
        public void TestValueMissing()
        {
            var json = "{ _id : 1 }";
            var c = BsonSerializer.Deserialize<RegisteredWithLambda>(json);
            Assert.Equal(1, c.Id);
            Assert.NotNull(c.L);
            Assert.Equal(0, c.L.Count);
        }

        [Fact]
        public void TestValuePresent()
        {
            var json = "{ _id : 1, L : [1] }";
            var c = BsonSerializer.Deserialize<RegisteredWithLambda>(json);
            Assert.Equal(1, c.Id);
            Assert.NotNull(c.L);
            Assert.Equal(1, c.L.Count);
            Assert.Equal(1, c.L[0]);
        }


        private class RegisteredWithLambda : IDefault
        {
            public int Id { get; set; }
            public List<int> L { get; set; }
            public int? Field { get; set; }
        }

        private class RegisteredWithMethod : IDefault
        {
            public int Id { get; set; }
            public List<int> L { get; set; }
        }


        private interface IDefault
        {
            int Id { get; set; }
            List<int> L { get; set; }
        }
    }
}
