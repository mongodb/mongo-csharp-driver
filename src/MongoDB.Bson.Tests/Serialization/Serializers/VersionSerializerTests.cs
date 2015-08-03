/* Copyright 2015 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    [TestFixture]
    public class VersionSerializerTests
    {
        [Test]
        public void TestDeserializeInvalidElement()
        {
            var json = "{ Invalid : 1 }";
            var exception = Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<Version>(json));
            var expectedMessage = "Invalid element: 'Invalid'.";
            Assert.AreEqual(expectedMessage, exception.Message);
        }

        [Test]
        public void TestDeserializeMissingMajor()
        {
            var json = "{ }";
            var exception = Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<Version>(json));
            var expectedMessage = "Missing element: 'Major'.";
            Assert.AreEqual(expectedMessage, exception.Message);
        }

        [Test]
        public void TestDeserializeMissingMinor()
        {
            var json = "{ Major : 1 }";
            var exception = Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<Version>(json));
            var expectedMessage = "Missing element: 'Minor'.";
            Assert.AreEqual(expectedMessage, exception.Message);
        }

        [Test]
        public void TestDeserializeMajorMinor()
        {
            var json = "{ Major : 1, Minor : 2 }";
            var version = BsonSerializer.Deserialize<Version>(json);
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(2, version.Minor);
            Assert.AreEqual(-1, version.Build);
            Assert.AreEqual(-1, version.Revision);
        }

        [Test]
        public void TestDeserializeMajorMinorOutOfOrder()
        {
            var json = "{ Minor : 2, Major : 1 }";
            var version = BsonSerializer.Deserialize<Version>(json);
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(2, version.Minor);
            Assert.AreEqual(-1, version.Build);
            Assert.AreEqual(-1, version.Revision);
        }

        [Test]
        public void TestDeserializeMajorMinorBuild()
        {
            var json = "{ Major : 1, Minor : 2, Build : 3 }";
            var version = BsonSerializer.Deserialize<Version>(json);
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(2, version.Minor);
            Assert.AreEqual(3, version.Build);
            Assert.AreEqual(-1, version.Revision);
        }

        [Test]
        public void TestDeserializeMajorMinorBuildOutOfOrder()
        {
            var json = "{ Build : 3, Major : 1, Minor : 2  }";
            var version = BsonSerializer.Deserialize<Version>(json);
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(2, version.Minor);
            Assert.AreEqual(3, version.Build);
            Assert.AreEqual(-1, version.Revision);
        }

        [Test]
        public void TestDeserializeMajorMinorBuildRevision()
        {
            var json = "{ Major : 1, Minor : 2, Build : 3, Revision : 4 }";
            var version = BsonSerializer.Deserialize<Version>(json);
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(2, version.Minor);
            Assert.AreEqual(3, version.Build);
            Assert.AreEqual(4, version.Revision);
        }

        [Test]
        public void TestDeserializeMajorMinorBuildRevisionOutOfOrder()
        {
            var json = "{ Revision : 4, Major : 1, Minor : 2, Build : 3 }";
            var version = BsonSerializer.Deserialize<Version>(json);
            Assert.AreEqual(1, version.Major);
            Assert.AreEqual(2, version.Minor);
            Assert.AreEqual(3, version.Build);
            Assert.AreEqual(4, version.Revision);
        }
    }
}
