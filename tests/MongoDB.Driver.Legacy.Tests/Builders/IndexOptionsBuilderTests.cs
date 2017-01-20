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

using System;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Builders
{
    public class IndexOptionsBuilderTests
    {
        [Fact]
        public void TestBackground()
        {
            var options = IndexOptions.SetBackground(true);
            string expected = "{ \"background\" : true }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestBits()
        {
            var options = IndexOptions.SetBits(32);
            string expected = "{ \"bits\" : 32 }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestDropDups()
        {
            var options = IndexOptions.SetDropDups(true);
            string expected = "{ \"dropDups\" : true }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestGeoSpatialRange()
        {
            var options = IndexOptions.SetGeoSpatialRange(1.5, 2.5);
            string expected = "{ \"min\" : 1.5, \"max\" : 2.5 }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestName()
        {
            var options = IndexOptions.SetName("custom");
            string expected = "{ \"name\" : \"custom\" }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestPartialFilterExpression()
        {
            var options = IndexOptions.SetPartialFilterExpression(Query.GT("x", 0));
            string expected = "{ \"partialFilterExpression\" : { \"x\" : { \"$gt\" : 0 } } }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestSparse()
        {
            var options = IndexOptions.SetSparse(true);
            string expected = "{ \"sparse\" : true }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestTimeToLive()
        {
            var options = IndexOptions.SetTimeToLive(TimeSpan.FromHours(1));
            string expected = "{ \"expireAfterSeconds\" : 3600 }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestUnique()
        {
            var options = IndexOptions.SetUnique(true);
            string expected = "{ \"unique\" : true }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestNameBackground()
        {
            var options = IndexOptions.SetName("custom").SetBackground(true);
            string expected = "{ \"name\" : \"custom\", \"background\" : true }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestNameDropDups()
        {
            var options = IndexOptions.SetName("custom").SetDropDups(true);
            string expected = "{ \"name\" : \"custom\", \"dropDups\" : true }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestNameGeoSpatialRange()
        {
            var options = IndexOptions.SetName("custom").SetGeoSpatialRange(1.5, 2.5);
            string expected = "{ \"name\" : \"custom\", \"min\" : 1.5, \"max\" : 2.5 }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestNameUnique()
        {
            var options = IndexOptions.SetName("custom").SetUnique(true);
            string expected = "{ \"name\" : \"custom\", \"unique\" : true }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestTextDefaultLanguage()
        {
            var options = IndexOptions.SetTextDefaultLanguage("spanish");
            string expected = "{ \"default_language\" : \"spanish\" }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestTextLanguageOverride()
        {
            var options = IndexOptions.SetTextLanguageOverride("idioma");
            string expected = "{ \"language_override\" : \"idioma\" }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestTextOptions()
        {
            var options = IndexOptions.SetName("custom").SetTextDefaultLanguage("spanish").SetTextLanguageOverride("idioma").SetWeight("a", 2);
            string expected = "{ \"name\" : \"custom\", \"default_language\" : \"spanish\", \"language_override\" : \"idioma\", \"weights\" : { \"a\" : 2 } }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestWeight()
        {
            var options = IndexOptions.SetWeight("a", 2);
            string expected = "{ \"weights\" : { \"a\" : 2 } }";
            Assert.Equal(expected, options.ToJson());
        }

        [Fact]
        public void TestMultipleWeights()
        {
            var options = IndexOptions.SetWeight("a", 2).SetWeight("b", 10);
            string expected = "{ \"weights\" : { \"a\" : 2, \"b\" : 10 } }";
            Assert.Equal(expected, options.ToJson());
        }
    }
}
