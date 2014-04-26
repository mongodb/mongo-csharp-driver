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
using MongoDB.Bson.Serialization.Options;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.Serialization
{
    [TestFixture]
    public class RepresentationSerializationOptionsTests
    {
        [Test]
        public void TestDefaults()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(false, options.AllowOverflow);
            Assert.AreEqual(false, options.AllowTruncation);
        }

        [Test]
        public void TestFalseFalse()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32, false, false);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(false, options.AllowOverflow);
            Assert.AreEqual(false, options.AllowTruncation);
        }

        [Test]
        public void TestFalseTrue()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32, false, true);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(false, options.AllowOverflow);
            Assert.AreEqual(true, options.AllowTruncation);
        }

        [Test]
        public void TestTrueFalse()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32, true, false);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(true, options.AllowOverflow);
            Assert.AreEqual(false, options.AllowTruncation);
        }

        [Test]
        public void TestTrueTrue()
        {
            var options = new RepresentationSerializationOptions(BsonType.Int32, true, true);
            Assert.AreEqual(BsonType.Int32, options.Representation);
            Assert.AreEqual(true, options.AllowOverflow);
            Assert.AreEqual(true, options.AllowTruncation);
        }
    }
}
