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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class AttributeConventionPackTests
    {
        [Fact]
        public void TestOptsInMembers()
        {
            var convention = AttributeConventionPack.Instance;
            var classMap = new BsonClassMap<TestClass>();
            new ConventionRunner(convention).Apply(classMap);

            Assert.Equal(1, classMap.DeclaredMemberMaps.Count());
            Assert.Equal("fn", classMap.GetMemberMap("_firstName").ElementName);
        }

        [Fact]
        public void TestThrowsWithDuplicateIds()
        {
            var convention = AttributeConventionPack.Instance;
            var classMap = new BsonClassMap<TestDuplicateIds>();

            Assert.Throws<DuplicateBsonMemberMapAttributeException>(() =>
                new ConventionRunner(convention).Apply(classMap));
        }

        [Fact]
        public void TestThrowsWithDuplicateExtraElements()
        {
            var convention = AttributeConventionPack.Instance;
            var classMap = new BsonClassMap<TestDuplicateExtraElements>();

            Assert.Throws<DuplicateBsonMemberMapAttributeException>(() =>
                new ConventionRunner(convention).Apply(classMap));
        }

        private class TestClass
        {
            [BsonElement("fn")]
            private string _firstName;

            public string FirstName
            {
                get { return _firstName; }
                set { _firstName = value; }
            }
        }

        private class TestDuplicateIds
        {
            [BsonId]
            public int Id { get; set; }

            [BsonId]
            public int AnotherId { get; set; }
        }

        private class TestDuplicateExtraElements
        {
            [BsonExtraElements]
            public Dictionary<string, object> ExtraElements { get; set; }

            [BsonExtraElements]
            public Dictionary<string, object> EE { get; set; }
        }
    }
}