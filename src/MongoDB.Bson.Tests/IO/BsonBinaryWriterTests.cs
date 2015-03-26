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

using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.IO
{
    [TestFixture]
    public class BsonBinaryWriterTests
    {
        [Test]
        public void BsonBinaryWriter_should_support_writing_multiple_documents(
            [Range(0, 3)]
            int numberOfDocuments)
        {
            var document = new BsonDocument("x", 1);
            var bson = document.ToBson();
            var expectedResult = Enumerable.Repeat(bson, numberOfDocuments).Aggregate(Enumerable.Empty<byte>(), (a, b) => a.Concat(b)).ToArray();

            using (var stream = new MemoryStream())
            using (var binaryWriter = new BsonBinaryWriter(stream))
            {
                for (var n = 0; n < numberOfDocuments; n++)
                {
                    binaryWriter.WriteStartDocument();
                    binaryWriter.WriteName("x");
                    binaryWriter.WriteInt32(1);
                    binaryWriter.WriteEndDocument();
                }

                var result = stream.ToArray();
                result.Should().Equal(expectedResult);
            }
        }
    }
}
