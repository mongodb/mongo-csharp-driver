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

using System;
using System.IO;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp351Tests
    {
        private class C
        {
            public int _id { get; set; }
            public N N { get; set; }
        }

        private class N
        {
            public int X { get; set; }
        }

        [Test]
        public void TestErrorMessage()
        {
            var json = "{ _id : 1, N : 'should be a document, not a string' }";
            try
            {
                var c = BsonSerializer.Deserialize<C>(json);
                Assert.Fail("Expected an exception to be thrown.");
            }
            catch (Exception ex)
            {
                var expected = "An error occurred while deserializing the N property of class MongoDB.BsonUnitTests.Jira.CSharp351Tests+C: Expected a nested document representing the serialized form of a MongoDB.BsonUnitTests.Jira.CSharp351Tests+N value, but found a value of type String instead.";
                Assert.IsInstanceOf<FileFormatException>(ex);
                Assert.IsInstanceOf<FileFormatException>(ex.InnerException);
                Assert.AreEqual(expected, ex.Message);
            }
        }
    }
}
