/* Copyright 2010-2016 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
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

        [Fact]
        public void TestMappingUsesMemberDefaultValueConvention()
        {
            var pack = new ConventionPack();
            pack.Add(new MemberDefaultValueConvention(typeof(int), 1));
            ConventionRegistry.Register("test", pack, t => t == typeof(A));

            var classMap = new BsonClassMap<A>(cm => cm.AutoMap());

            var defaultValue = classMap.GetMemberMap("Match").DefaultValue;
            Assert.IsType<int>(defaultValue);
            Assert.Equal(1, defaultValue);
        }

        [Fact]
        public void TestMappingUsesMemberDefaultValueConventionDoesNotMatchWrongProperty()
        {
            var pack = new ConventionPack();
            pack.Add(new MemberDefaultValueConvention(typeof(int), 1));
            ConventionRegistry.Register("test", pack, t => t == typeof(A));

            var classMap = new BsonClassMap<A>(cm => cm.AutoMap());

            var defaultValue = classMap.GetMemberMap("NoMatch").DefaultValue;
            Assert.Equal(0L, defaultValue);
        }

        [Fact]
        public void TestMappingUsesMemberDefaultValueConventionDoesNotOverrideAttribute()
        {
            var pack = new ConventionPack();
            pack.Add(new MemberDefaultValueConvention(typeof(int), 1));
            ConventionRegistry.Register("test", pack, t => t == typeof(B));

            var classMap = new BsonClassMap<B>(cm => cm.AutoMap());

            var defaultValue = classMap.GetMemberMap("Match").DefaultValue;
            Assert.IsType<int>(defaultValue);
            Assert.Equal(2, defaultValue);
        }
    }
}
