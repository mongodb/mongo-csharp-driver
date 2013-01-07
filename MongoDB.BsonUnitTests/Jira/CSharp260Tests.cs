/* Copyright 2010-2013 10gen Inc.
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
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira.CSharp260
{
    [TestFixture]
    public class CSharp260Tests
    {
        [Test]
        public void TestConstantPattern()
        {
            var json = "{ rx : /abc/ }";
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.AreEqual("abc", rx.Pattern);
            Assert.AreEqual("", rx.Options);
        }

        [Test]
        public void TestConstantPatternWithOptions()
        {
            var json = "{ rx : /abc/imxs }";
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.AreEqual("abc", rx.Pattern);
            Assert.AreEqual("imxs", rx.Options);
        }

        [Test]
        public void TestNewRegExpPattern()
        {
            var json = "{ rx : new RegExp('abc') }";
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.AreEqual("abc", rx.Pattern);
            Assert.AreEqual("", rx.Options);
        }

        [Test]
        public void TestNewRegExpPatternWithOptions()
        {
            var json = "{ rx : new RegExp('abc', 'imxs') }";
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.AreEqual("abc", rx.Pattern);
            Assert.AreEqual("imxs", rx.Options);
        }

        [Test]
        public void TestRegExpPattern()
        {
            var json = "{ rx : RegExp('abc') }";
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.AreEqual("abc", rx.Pattern);
            Assert.AreEqual("", rx.Options);
        }

        [Test]
        public void TestRegExpPatternWithOptions()
        {
            var json = "{ rx : RegExp('abc', 'imxs') }";
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.AreEqual("abc", rx.Pattern);
            Assert.AreEqual("imxs", rx.Options);
        }

        [Test]
        public void TestStrictPattern()
        {
            var json = "{ rx : { $regex : 'abc' } }";
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.AreEqual("abc", rx.Pattern);
            Assert.AreEqual("", rx.Options);
        }

        [Test]
        public void TestStrictPatternWithOptions()
        {
            var json = "{ rx : { $regex : 'abc', $options : 'imxs' } }";
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.RegularExpression, document["rx"].BsonType);
            var rx = document["rx"].AsBsonRegularExpression;
            Assert.AreEqual("abc", rx.Pattern);
            Assert.AreEqual("imxs", rx.Options);
        }
    }
}
