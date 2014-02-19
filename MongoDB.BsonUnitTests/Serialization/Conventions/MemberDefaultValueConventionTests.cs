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

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization.Conventions
{
    [TestFixture]
    public class MemberDefaultValueConventionTests
    {
        private class A
        {
            public int Match { get; set; }
            public long NoMatch { get; set; }
        }

        private class B
        {
            [BsonDefaultValue(2)]
            public int Match { get; set; }
        }

        [Test]
        public void TestMappingUsesMemberDefaultValueConvention()
        {
            var pack = new ConventionPack();
            pack.Add(new MemberDefaultValueConvention(typeof(int), 1));
            ConventionRegistry.Register("test", pack, t => t == typeof(A));

            var classMap = new BsonClassMap<A>(cm => cm.AutoMap());

            var defaultValue = classMap.GetMemberMap("Match").DefaultValue;
            Assert.IsInstanceOf<int>(defaultValue);
            Assert.AreEqual(1, defaultValue);
        }

        [Test]
        public void TestMappingUsesMemberDefaultValueConventionDoesNotMatchWrongProperty()
        {
            var pack = new ConventionPack();
            pack.Add(new MemberDefaultValueConvention(typeof(int), 1));
            ConventionRegistry.Register("test", pack, t => t == typeof(A));

            var classMap = new BsonClassMap<A>(cm => cm.AutoMap());

            var defaultValue = classMap.GetMemberMap("NoMatch").DefaultValue;
            Assert.AreEqual(0, defaultValue);
        }

        [Test]
        public void TestMappingUsesMemberDefaultValueConventionDoesNotOverrideAttribute()
        {
            var pack = new ConventionPack();
            pack.Add(new MemberDefaultValueConvention(typeof(int), 1));
            ConventionRegistry.Register("test", pack, t => t == typeof(B));

            var classMap = new BsonClassMap<B>(cm => cm.AutoMap());

            var defaultValue = classMap.GetMemberMap("Match").DefaultValue;
            Assert.IsInstanceOf<int>(defaultValue);
            Assert.AreEqual(2, defaultValue);
        }
    }
}
