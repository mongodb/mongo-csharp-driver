/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Builders
{
    [TestFixture]
    public class CollectionOptionsBuilderTests
    {
        [Test]
        public void TestSetAll()
        {
            var options = CollectionOptions
                .SetAutoIndexId(true)
                .SetCapped(true)
                .SetMaxDocuments(100)
                .SetMaxSize(2000);
            var expected = "{ 'autoIndexId' : true, 'capped' : true, 'max' : NumberLong(100), 'size' : NumberLong(2000) }".Replace("'", "\"");
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetAutoIndexIdFalse()
        {
            var options = CollectionOptions.SetAutoIndexId(false);
            var expected = "{ 'autoIndexId' : false }".Replace("'", "\"");
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetAutoIndexIdTrue()
        {
            var options = CollectionOptions.SetAutoIndexId(true);
            var expected = "{ 'autoIndexId' : true }".Replace("'", "\"");
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetCappedFalse()
        {
            var options = CollectionOptions.SetCapped(false);
            var expected = "{ 'capped' : false }".Replace("'", "\"");
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetCappedTrue()
        {
            var options = CollectionOptions.SetCapped(true);
            var expected = "{ 'capped' : true }".Replace("'", "\"");
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetIndexOptionDefaults()
        {
            var options = CollectionOptions.SetIndexOptionDefaults(new IndexOptionDefaults { StorageEngine = new BsonDocument("mmapv1", new BsonDocument()) });
            var expected = "{ \"indexOptionDefaults\" : { \"storageEngine\" : { \"mmapv1\" : { } } } }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetMaxDocuments()
        {
            var options = CollectionOptions.SetMaxDocuments(100);
            var expected = "{ 'max' : NumberLong(100) }".Replace("'", "\"");
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetMaxSize()
        {
            var options = CollectionOptions.SetMaxSize(2147483649);
            var expected = "{ 'size' : NumberLong('2147483649') }".Replace("'", "\"");
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetNone()
        {
            var options = new CollectionOptionsBuilder();
            var expected = "{ }".Replace("'", "\"");
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetValidationAction()
        {
            var options = CollectionOptions.SetValidationAction(DocumentValidationAction.Error);
            var expected = "{ \"validationAction\" : \"error\" }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetValidationLevel()
        {
            var options = CollectionOptions.SetValidationLevel(DocumentValidationLevel.Strict);
            var expected = "{ \"validationLevel\" : \"strict\" }";
            Assert.AreEqual(expected, options.ToJson());
        }

        [Test]
        public void TestSetValidator()
        {
            var options = CollectionOptions.SetValidator(new QueryDocument("_id", new BsonDocument("$exists", true)));
            var expected = "{ \"validator\" : { \"_id\" : { \"$exists\" : true } } }";
            Assert.AreEqual(expected, options.ToJson());
        }
    }
}
