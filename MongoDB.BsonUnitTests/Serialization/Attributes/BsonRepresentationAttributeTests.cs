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
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization.Attributes
{
    [TestFixture]
    public class BsonRepresentationAttributeTests
    {
#pragma warning disable 649 // never assigned to
        private class C
        {
            public int I;
            [BsonRepresentation(BsonType.Int64)]
            public int IL;
            [BsonRepresentation(BsonType.Int32)]
            public long LI;
            [BsonRepresentation(BsonType.Int32, AllowOverflow = true)]
            public long LIO;
            [BsonRepresentation(BsonType.Int32, AllowTruncation = true)]
            public double DIT;
            [BsonRepresentation(BsonType.Int32, AllowOverflow = true, AllowTruncation = true)]
            public double DIOT;
        }
#pragma warning restore

        [Test]
        public void TestRepresentationAttributeForI()
        {
            var fieldInfo = typeof(C).GetField("I");
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false);
            Assert.AreEqual(0, attributes.Length);
        }

        [Test]
        public void TestRepresentationAttributeForIL()
        {
            var fieldInfo = typeof(C).GetField("IL");
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false);
            Assert.AreEqual(1, attributes.Length);
            var attribute = (BsonRepresentationAttribute)attributes[0];
            Assert.AreEqual(BsonType.Int64, attribute.Representation);
            Assert.AreEqual(false, attribute.AllowOverflow);
            Assert.AreEqual(false, attribute.AllowTruncation);
        }

        [Test]
        public void TestRepresentationAttributeForLI()
        {
            var fieldInfo = typeof(C).GetField("LI");
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false);
            Assert.AreEqual(1, attributes.Length);
            var attribute = (BsonRepresentationAttribute)attributes[0];
            Assert.AreEqual(BsonType.Int32, attribute.Representation);
            Assert.AreEqual(false, attribute.AllowOverflow);
            Assert.AreEqual(false, attribute.AllowTruncation);
        }

        [Test]
        public void TestRepresentationAttributeForLIO()
        {
            var fieldInfo = typeof(C).GetField("LIO");
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false);
            Assert.AreEqual(1, attributes.Length);
            var attribute = (BsonRepresentationAttribute)attributes[0];
            Assert.AreEqual(BsonType.Int32, attribute.Representation);
            Assert.AreEqual(true, attribute.AllowOverflow);
            Assert.AreEqual(false, attribute.AllowTruncation);
        }

        [Test]
        public void TestRepresentationAttributeForDIT()
        {
            var fieldInfo = typeof(C).GetField("DIT");
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false);
            Assert.AreEqual(1, attributes.Length);
            var attribute = (BsonRepresentationAttribute)attributes[0];
            Assert.AreEqual(BsonType.Int32, attribute.Representation);
            Assert.AreEqual(false, attribute.AllowOverflow);
            Assert.AreEqual(true, attribute.AllowTruncation);
        }

        [Test]
        public void TestRepresentationAttributeForDIOT()
        {
            var fieldInfo = typeof(C).GetField("DIOT");
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false);
            Assert.AreEqual(1, attributes.Length);
            var attribute = (BsonRepresentationAttribute)attributes[0];
            Assert.AreEqual(BsonType.Int32, attribute.Representation);
            Assert.AreEqual(true, attribute.AllowOverflow);
            Assert.AreEqual(true, attribute.AllowTruncation);
        }
    }
}
