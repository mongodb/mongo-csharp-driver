/* Copyright 2010-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson.IO;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class DateTimeJsonTokenTests
    {
        [Fact]
        public void Constructor_should_initialize_token_with_provided_values()
        {
            // Arrange
            var lexeme = "ISODate(\"2023-01-01T00:00:00Z\")";
            var value = new BsonDateTime(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            // Act
            var token = new DateTimeJsonToken(lexeme, value);

            // Assert
            token.Lexeme.Should().Be(lexeme);
            token.Type.Should().Be(JsonTokenType.DateTime);
        }

        [Fact]
        public void Constructor_should_set_token_type_to_DateTime()
        {
            // Arrange
            var lexeme = "ISODate(\"2023-01-01T00:00:00Z\")";
            var value = new BsonDateTime(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            // Act
            var token = new DateTimeJsonToken(lexeme, value);

            // Assert
            token.Type.Should().Be(JsonTokenType.DateTime);
        }

        [Fact]
        public void DateTimeValue_should_return_provided_value()
        {
            // Arrange
            var lexeme = "ISODate(\"2023-01-01T00:00:00Z\")";
            var value = new BsonDateTime(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var token = new DateTimeJsonToken(lexeme, value);

            // Act
            var result = token.DateTimeValue;

            // Assert
            result.Should().Be(value);
        }
    }
}
