/* Copyright 2021-present MongoDB Inc.
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
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class WriteConcernErrorTests
    {
        [Fact]
        public void constructor_should_initialize_subject()
        {
            var code = 1;
            var codeName = "47";
            var message = "Error message";
            var details = new BsonDocument
            {
                { "details", new BsonDocument() }
            };
            var errorLabels = new[] { "1", "2" };

            var subject = new WriteConcernError(code, codeName, message, details, errorLabels);

            subject.Code.Should().Be(code);
            subject.CodeName.Should().Be(codeName);
            subject.Message.Should().Be(message);
            subject.Details.Should().Be(details);
            subject.ErrorLabels.Should().BeEquivalentTo(errorLabels);
        }

        [Theory]
        [InlineData(0, null, null, null, null, "{ Code : \"0\" }")]
        [InlineData(1, null, "", null, new string[0], "{ Code : \"1\", Message : \"\" }")]
        [InlineData(2, "two", "err", "{ a : 3 }", new[] { "x", "y", "z" }, "{ Code : \"2\", CodeName : \"two\", Message : \"err\", Details : \"{ \"a\" : 3 }\", ErrorLabels : [ \"x\", \"y\", \"z\" ] }")]
        public void ToString_should_return_expected_result(
            int code,
            string codeName,
            string message,
            string detailsJson,
            string[] errorLabels,
            string expectedResult)
        {
            var details = detailsJson == null ? null : BsonDocument.Parse(detailsJson);
            var writeConcernError = new WriteConcernError(code, codeName, message, details, errorLabels);

            var result = writeConcernError.ToString();

            result.Should().Be(expectedResult);
        }
    }
}
