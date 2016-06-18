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

using System.Linq;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Attributes
{
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

        [Fact]
        public void TestRepresentationAttributeForI()
        {
            var fieldInfo = typeof(C).GetField("I");
#if NETCORE
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false).ToArray();
#else
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false);
#endif
            Assert.Equal(0, attributes.Length);
        }

        [Fact]
        public void TestRepresentationAttributeForIL()
        {
            var fieldInfo = typeof(C).GetField("IL");
#if NETCORE
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false).ToArray();
#else
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false);
#endif
            Assert.Equal(1, attributes.Length);
            var attribute = (BsonRepresentationAttribute)attributes[0];
            Assert.Equal(BsonType.Int64, attribute.Representation);
            Assert.Equal(false, attribute.AllowOverflow);
            Assert.Equal(false, attribute.AllowTruncation);
        }

        [Fact]
        public void TestRepresentationAttributeForLI()
        {
            var fieldInfo = typeof(C).GetField("LI");
#if NETCORE
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false).ToArray();
#else
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false);
#endif
            Assert.Equal(1, attributes.Length);
            var attribute = (BsonRepresentationAttribute)attributes[0];
            Assert.Equal(BsonType.Int32, attribute.Representation);
            Assert.Equal(false, attribute.AllowOverflow);
            Assert.Equal(false, attribute.AllowTruncation);
        }

        [Fact]
        public void TestRepresentationAttributeForLIO()
        {
            var fieldInfo = typeof(C).GetField("LIO");
#if NETCORE
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false).ToArray();
#else
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false);
#endif
            Assert.Equal(1, attributes.Length);
            var attribute = (BsonRepresentationAttribute)attributes[0];
            Assert.Equal(BsonType.Int32, attribute.Representation);
            Assert.Equal(true, attribute.AllowOverflow);
            Assert.Equal(false, attribute.AllowTruncation);
        }

        [Fact]
        public void TestRepresentationAttributeForDIT()
        {
            var fieldInfo = typeof(C).GetField("DIT");
#if NETCORE
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false).ToArray();
#else
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false);
#endif
            Assert.Equal(1, attributes.Length);
            var attribute = (BsonRepresentationAttribute)attributes[0];
            Assert.Equal(BsonType.Int32, attribute.Representation);
            Assert.Equal(false, attribute.AllowOverflow);
            Assert.Equal(true, attribute.AllowTruncation);
        }

        [Fact]
        public void TestRepresentationAttributeForDIOT()
        {
            var fieldInfo = typeof(C).GetField("DIOT");
#if NETCORE
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false).ToArray();
#else
            var attributes = fieldInfo.GetCustomAttributes(typeof(BsonRepresentationAttribute), false);
#endif
            Assert.Equal(1, attributes.Length);
            var attribute = (BsonRepresentationAttribute)attributes[0];
            Assert.Equal(BsonType.Int32, attribute.Representation);
            Assert.Equal(true, attribute.AllowOverflow);
            Assert.Equal(true, attribute.AllowTruncation);
        }
    }
}
