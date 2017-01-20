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
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class IdGeneratorConventionsTests
    {
        private class TestClassA
        {
            public ObjectId ObjectId { get; set; }
        }

        private class TestClassB
        {
            public Guid GuidId { get; set; }
        }

        [Fact]
        public void TestLookupIdGeneratorConventionWithTestClassA()
        {
            var convention = new LookupIdGeneratorConvention();
            var classMap = new BsonClassMap<TestClassA>();
            classMap.MapIdMember(x => x.ObjectId);
            convention.PostProcess(classMap);
            Assert.NotNull(classMap.IdMemberMap.IdGenerator);
            Assert.IsType<ObjectIdGenerator>(classMap.IdMemberMap.IdGenerator);
        }

        [Fact]
        public void TestLookupIdGeneratorConventionWithTestClassB()
        {
            var convention = new LookupIdGeneratorConvention();
            var classMap = new BsonClassMap<TestClassB>();
            classMap.MapIdMember(x => x.GuidId);
            convention.PostProcess(classMap);
            Assert.NotNull(classMap.IdMemberMap.IdGenerator);
            Assert.IsType<GuidGenerator>(classMap.IdMemberMap.IdGenerator);
        }
    }
}
