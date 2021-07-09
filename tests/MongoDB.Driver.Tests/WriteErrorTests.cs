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
    public class WriteErrorTests
    {
        [Fact]
        public void constructor_should_initialize_subject()
        {
            var category = ServerErrorCategory.Uncategorized;
            var code = 1;
            var message = "Document failed validation";
            var details = new BsonDocument
            {
                { "failingDocumentId", new ObjectId() },
                { "details", new BsonDocument() },
                { "reason", "reason" }
            };

            var subject = new WriteError(category, code, message, details);

            subject.Category.Should().Be(category);
            subject.Message.Should().Be(message);
            subject.Details.Should().Be(details);
        }

        [Theory]
        [InlineData(ServerErrorCategory.DuplicateKey, 1, "1", null, "{ Category : \"DuplicateKey\", Code : 1, Message : \"1\" }")]
        [InlineData(ServerErrorCategory.ExecutionTimeout, 101, null, "{ consideredValue : 1 }", "{ Category : \"ExecutionTimeout\", Code : 101, Details : \"{ \"consideredValue\" : 1 }\" }")]
        [InlineData(ServerErrorCategory.Uncategorized, 303, "0", "{ specifiedAs : { x : { $type : \"string\" } } }", "{ Category : \"Uncategorized\", Code : 303, Message : \"0\", Details : \"{ \"specifiedAs\" : { \"x\" : { \"$type\" : \"string\" } } }\" }")]
        public void ToString_should_return_expected_result(
            ServerErrorCategory category,
            int code,
            string message,
            string detailsJson,
            string expectedResult)
        {
            var details = detailsJson == null ? null : BsonDocument.Parse(detailsJson);
            var writeError = new WriteError(category, code, message, details);

            var result = writeError.ToString();

            result.Should().Be(expectedResult);
        }
    }
}
