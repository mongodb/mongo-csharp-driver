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

using System;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Builders
{
    public class IndexOptionsBuilderTypedTests
    {
        public class TestClass
        {
            public int _id;
            public string textfield;
            public string idioma;
            public Details details = new Details();
        }

        public class Details
        {
            public string text;
        }

        [Fact]
        public void TestBackground()
        {
            var options = IndexOptions<TestClass>.SetBackground(true);
            string expected = "{ \"background\" : true }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestBits()
        {
            var options = IndexOptions<TestClass>.SetBits(32);
            string expected = "{ \"bits\" : 32 }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestDropDups()
        {
            var options = IndexOptions<TestClass>.SetDropDups(true);
            string expected = "{ \"dropDups\" : true }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestGeoSpatialRange()
        {
            var options = IndexOptions<TestClass>.SetGeoSpatialRange(1.5, 2.5);
            string expected = "{ \"min\" : 1.5, \"max\" : 2.5 }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestName()
        {
            var options = IndexOptions<TestClass>.SetName("custom");
            string expected = "{ \"name\" : \"custom\" }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestPartialFilterExpression()
        {
            var options = IndexOptions<TestClass>.SetPartialFilterExpression(Query.GT("x", 0));
            string expected = "{ \"partialFilterExpression\" : { \"x\" : { \"$gt\" : 0 } } }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestSparse()
        {
            var options = IndexOptions<TestClass>.SetSparse(true);
            string expected = "{ \"sparse\" : true }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestTimeToLive()
        {
            var options = IndexOptions<TestClass>.SetTimeToLive(TimeSpan.FromHours(1));
            string expected = "{ \"expireAfterSeconds\" : 3600 }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestUnique()
        {
            var options = IndexOptions<TestClass>.SetUnique(true);
            string expected = "{ \"unique\" : true }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestNameBackground()
        {
            var options = IndexOptions<TestClass>.SetName("custom").SetBackground(true);
            string expected = "{ \"name\" : \"custom\", \"background\" : true }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestNameDropDups()
        {
            var options = IndexOptions<TestClass>.SetName("custom").SetDropDups(true);
            string expected = "{ \"name\" : \"custom\", \"dropDups\" : true }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestNameGeoSpatialRange()
        {
            var options = IndexOptions<TestClass>.SetName("custom").SetGeoSpatialRange(1.5, 2.5);
            string expected = "{ \"name\" : \"custom\", \"min\" : 1.5, \"max\" : 2.5 }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestNameUnique()
        {
            var options = IndexOptions<TestClass>.SetName("custom").SetUnique(true);
            string expected = "{ \"name\" : \"custom\", \"unique\" : true }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestTextDefaultLanguage()
        {
            var options = IndexOptions<TestClass>.SetTextDefaultLanguage("spanish");
            string expected = "{ \"default_language\" : \"spanish\" }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestTextLanguageOverride()
        {
            var options = IndexOptions<TestClass>.SetTextLanguageOverride(x => x.idioma);
            string expected = "{ \"language_override\" : \"idioma\" }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestTextOptions()
        {
            var options = IndexOptions<TestClass>.SetName("custom").SetTextDefaultLanguage("spanish").SetTextLanguageOverride(x => x.idioma).SetWeight(x => x.textfield, 2);
            string expected = "{ \"name\" : \"custom\", \"default_language\" : \"spanish\", \"language_override\" : \"idioma\", \"weights\" : { \"textfield\" : 2 } }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestWeight()
        {
            var options = IndexOptions<TestClass>.SetWeight(x => x.textfield, 2);
            string expected = "{ \"weights\" : { \"textfield\" : 2 } }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestMultipleWeights()
        {
            var options = IndexOptions<TestClass>.SetWeight(x => x.textfield, 2).SetWeight(x => x.idioma, 10);
            string expected = "{ \"weights\" : { \"textfield\" : 2, \"idioma\" : 10 } }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void WildcardProjection_should_return_expected_result()
        {
            var options = IndexOptions<TestClass>.SetWildcardProjection(x => x.textfield, true);
            BsonDocument expected = BsonDocument.Parse("{ \"wildcardProjection\" : { \"textfield\" : 1 } }");
            options.ToBsonDocument().Should().Be(expected);

            options = IndexOptions<TestClass>
                .SetWildcardProjection(x => x._id, false)
                .SetWildcardProjection(x => x.details.text, true);
            expected = BsonDocument.Parse("{ \"wildcardProjection\" : { \"_id\" : 0, \"details.text\" : 1 } }");
            options.ToBsonDocument().Should().Be(expected);
        }
    }
}
