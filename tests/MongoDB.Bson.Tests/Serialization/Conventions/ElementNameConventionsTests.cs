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

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class ElementNameConventionsTests
    {
        [Fact]
        public void TestBsonElementAttributeOverridesNamedIdMemberConvention()
        {
            var classMap = new BsonClassMap<BookWithBsonElementAttribute>();
            classMap.AutoMap();

            classMap.Freeze();
            Assert.Null(classMap.IdMemberMap);
            Assert.Equal("notId", classMap.GetMemberMap(x => x.Id).ElementName);
        }

        [Fact]
        public void TestBsonElementWithoutNameOnIdPropertyStillBecomesIdMember()
        {
            var classMap = new BsonClassMap<BookWithBsonElementAttributeNoName>();
            classMap.AutoMap();

            classMap.Freeze();
            Assert.Equal("Id", classMap.IdMemberMap.MemberName);
            Assert.Equal("_id", classMap.GetMemberMap(x => x.Id).ElementName);
        }

        [Fact]
        public void TestBsonIdAttributeOverridesNamedIdMemberConvention()
        {
            var classMap = new BsonClassMap<BookWithBsonIdAttribute>();
            classMap.AutoMap();

            classMap.Freeze();
            Assert.Equal("_id", classMap.GetMemberMap(x => x.Title).ElementName);
            Assert.Equal("_id", classMap.IdMemberMap.ElementName);
            Assert.Equal("Title", classMap.IdMemberMap.MemberName);
            Assert.Equal("id", classMap.GetMemberMap(x => x.id).ElementName);
        }

        [Fact]
        public void TestBsonIdAttributeWinsWhenBsonElementSkipsIdProperty()
        {
            var classMap = new BsonClassMap<BookWithBsonElementAndBsonId>();
            classMap.AutoMap();
            classMap.Freeze();

            Assert.Equal("Title", classMap.IdMemberMap.MemberName);
            Assert.Equal("notId", classMap.GetMemberMap(x => x.Id).ElementName);
        }

        [Fact]
        public void TestBsonElementWithUnderscoreIdOnIdPropertyAlsoBecomesIdMember()
        {
            var classMap = new BsonClassMap<BookWithExplicitBsonElementId>();
            classMap.AutoMap();
            classMap.Freeze();

            Assert.NotNull(classMap.IdMemberMap);
            Assert.Equal("Id", classMap.IdMemberMap.MemberName);
            Assert.Equal("_id", classMap.GetMemberMap(x => x.Id).ElementName);
        }

        [Fact]
        public void TestBsonElementWithUnderscoreIdButNoIdPropertyDoesNotBecomeIdMember()
        {
            var classMap = new BsonClassMap<BookWithExplicitBsonElementIdButNoIdProperty>();
            classMap.AutoMap();
            classMap.Freeze();

            Assert.Null(classMap.IdMemberMap);
            Assert.Equal("_id", classMap.GetMemberMap(x => x.Title).ElementName);
        }

        [Fact]
        public void TestBsonElementOnIdFallsThroughToLowercaseId()
        {
            var classMap = new BsonClassMap<BookWithBsonElementAndFallthrough>();
            classMap.AutoMap();
            classMap.Freeze();

            Assert.Equal("id", classMap.IdMemberMap.MemberName);
            Assert.Equal("_id", classMap.GetMemberMap(x => x.id).ElementName);
            Assert.Equal("notId", classMap.GetMemberMap(x => x.Id).ElementName);
        }

        [Fact]
        public void TestMemberNameElementNameConvention()
        {
            var convention = new MemberNameElementNameConvention();
            var classMap = new BsonClassMap<TestClass>();
            convention.Apply(classMap.MapMember(x => x.FirstName));
            convention.Apply(classMap.MapMember(x => x.Age));
            convention.Apply(classMap.MapMember(x => x._DumbName));
            convention.Apply(classMap.MapMember(x => x.lowerCase));
            Assert.Equal("FirstName", classMap.GetMemberMap(x => x.FirstName).ElementName);
            Assert.Equal("Age", classMap.GetMemberMap(x => x.Age).ElementName);
            Assert.Equal("_DumbName", classMap.GetMemberMap(x => x._DumbName).ElementName);
            Assert.Equal("lowerCase", classMap.GetMemberMap(x => x.lowerCase).ElementName);
        }

        [Fact]
        public void TestCamelCaseElementNameConvention()
        {
            var convention = new CamelCaseElementNameConvention();
            var classMap = new BsonClassMap<TestClass>();
            convention.Apply(classMap.MapMember(x => x.FirstName));
            convention.Apply(classMap.MapMember(x => x.Age));
            convention.Apply(classMap.MapMember(x => x._DumbName));
            convention.Apply(classMap.MapMember(x => x.lowerCase));
            Assert.Equal("firstName", classMap.GetMemberMap(x => x.FirstName).ElementName);
            Assert.Equal("age", classMap.GetMemberMap(x => x.Age).ElementName);
            Assert.Equal("_DumbName", classMap.GetMemberMap(x => x._DumbName).ElementName);
            Assert.Equal("lowerCase", classMap.GetMemberMap(x => x.lowerCase).ElementName);
        }

        private class BookWithBsonElementAttribute
        {
            [BsonElement("notId")]
            public ObjectId Id { get; set; } // should not be set to _id
        }

        private class BookWithBsonIdAttribute
        {
            public ObjectId id { get; set; }

            [BsonId]
            public string Title { get; set; } // should be set to _id
        }

        private class BookWithBsonElementAndBsonId
        {
            [BsonElement("notId")]
            public ObjectId Id { get; set; }

            [BsonId]
            public string Title { get; set; } // should be set to _id
        }

        private class BookWithExplicitBsonElementId
        {
            [BsonElement("_id")]
            public string Id { get; set; } // should be set to _id
        }

        private class BookWithExplicitBsonElementIdButNoIdProperty
        {
            [BsonElement("_id")]
            public string Title { get; set; } // should be set to _id
        }

        private class BookWithBsonElementAndFallthrough
        {
            [BsonElement("notId")]
            public ObjectId Id { get; set; }
            public ObjectId id { get; set; } // should be set to _id
        }

        private class BookWithBsonElementAttributeNoName
        {
            [BsonElement]
            public ObjectId Id { get; set; } // should be set to _id
        }

        private class TestClass
        {
            public string FirstName { get; set; }
            public int Age { get; set; }
            public string _DumbName { get; set; }
            public string lowerCase { get; set; }
        }
    }
}
