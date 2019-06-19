/* Copyright 2019-present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp2643Tests
    {
        [Theory]
        [InlineData("{ }", 0, 0)]
        [InlineData("{ X : 1 }", 1, 0)]
        [InlineData("{ Y : 2 }", 0, 2)]
        [InlineData("{ X : 1, Y : 2 }", 1, 2)]
        public void Deserializer_should_return_expected_result(string json, int expectedX, int expectedY)
        {
            var prototype = new { CSharp2643 = true, X = 1, Y = 2 }; // use CSharp2643 as a property name to guarantee a unique anonymous type

            T deserialize<T>(T dummy, string value)
            {
                var serializer = BsonSerializer.LookupSerializer<T>();
                using (var reader = new JsonReader(value))
                {
                    var context = BsonDeserializationContext.CreateRoot(reader);
                    return serializer.Deserialize(context);
                }
            }

            var result = deserialize(prototype, json);

            result.CSharp2643.Should().BeFalse();
            result.X.Should().Be(expectedX);
            result.Y.Should().Be(expectedY);
        }
    }
}
