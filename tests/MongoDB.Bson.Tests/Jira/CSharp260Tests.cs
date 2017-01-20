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

using MongoDB.Bson;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp260
{
    public class CSharp260Tests
    {
        [Fact]
        public void TestConstantPattern()
        {
            var json = "{ rx : /abc/ }";
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.Equal("abc", rx.Pattern);
            Assert.Equal("", rx.Options);
        }

        [Fact]
        public void TestConstantPatternWithOptions()
        {
            var json = "{ rx : /abc/imxs }";
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.Equal("abc", rx.Pattern);
            Assert.Equal("imxs", rx.Options);
        }

        [Fact]
        public void TestNewRegExpPattern()
        {
            var json = "{ rx : new RegExp('abc') }";
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.Equal("abc", rx.Pattern);
            Assert.Equal("", rx.Options);
        }

        [Fact]
        public void TestNewRegExpPatternWithOptions()
        {
            var json = "{ rx : new RegExp('abc', 'imxs') }";
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.Equal("abc", rx.Pattern);
            Assert.Equal("imxs", rx.Options);
        }

        [Fact]
        public void TestRegExpPattern()
        {
            var json = "{ rx : RegExp('abc') }";
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.Equal("abc", rx.Pattern);
            Assert.Equal("", rx.Options);
        }

        [Fact]
        public void TestRegExpPatternWithOptions()
        {
            var json = "{ rx : RegExp('abc', 'imxs') }";
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.Equal("abc", rx.Pattern);
            Assert.Equal("imxs", rx.Options);
        }

        [Fact]
        public void TestStrictPattern()
        {
            var json = "{ rx : { $regex : 'abc' } }";
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.Equal("abc", rx.Pattern);
            Assert.Equal("", rx.Options);
        }

        [Fact]
        public void TestStrictPatternWithOptions()
        {
            var json = "{ rx : { $regex : 'abc', $options : 'imxs' } }";
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.Equal("abc", rx.Pattern);
            Assert.Equal("imxs", rx.Options);
        }
    }
}
