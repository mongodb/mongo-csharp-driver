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
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class IdMemberConventionsTests
    {
        private class TestClassA
        {
            public Guid Id { get; set; }
            public ObjectId OtherId { get; set; }
        }

        private class TestClassB
        {
            public ObjectId OtherId { get; set; }
        }

        private class TestClassC
        {
            public ObjectId id { get; set; }
        }

        private class TestClassD
        {
            public ObjectId _id { get; set; }
        }

        [Fact]
        public void TestNamedIdMemberConventionWithTestClassA()
        {
            var convention = new NamedIdMemberConvention("Id", "id", "_id");
            var classMap = new BsonClassMap<TestClassA>();
            convention.Apply(classMap);
            Assert.NotNull(classMap.IdMemberMap);
            Assert.Equal("Id", classMap.IdMemberMap.MemberName);
        }

        [Fact]
        public void TestNamedIdMemberConventionWithTestClassB()
        {
            var convention = new NamedIdMemberConvention("Id", "id", "_id");
            var classMap = new BsonClassMap<TestClassB>();
            convention.Apply(classMap);
            Assert.Null(classMap.IdMemberMap);
        }

        [Fact]
        public void TestNamedIdMemberConventionWithTestClassC()
        {
            var convention = new NamedIdMemberConvention("Id", "id", "_id");
            var classMap = new BsonClassMap<TestClassC>();
            convention.Apply(classMap);
            Assert.NotNull(classMap.IdMemberMap);
            Assert.Equal("id", classMap.IdMemberMap.MemberName);
        }

        [Fact]
        public void TestNamedIdMemberConventionWithTestClassD()
        {
            var convention = new NamedIdMemberConvention("Id", "id", "_id");
            var classMap = new BsonClassMap<TestClassD>();
            convention.Apply(classMap);
            Assert.NotNull(classMap.IdMemberMap);
            Assert.Equal("_id", classMap.IdMemberMap.MemberName);
        }
    }
}
