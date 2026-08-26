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
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Ast.Filters
{
    public class AstImpliedOperationFilterOperationTests
    {
        [Theory]
        [InlineData("{ $ne : 1 }")]
        [InlineData("{ $gt : 1 }")]
        [InlineData("{ $exists : true }")]
        [InlineData("{ $where : 'sleep(1)' }")]
        [InlineData("{ $ne : 1, x : 2 }")] // the first element name is what decides
        public void CanRepresent_should_return_false_for_an_operator_document(string valueAsJson)
        {
            var value = ParseValue(valueAsJson);

            var result = AstImpliedOperationFilterOperation.CanRepresent(value);

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("{ }")] // an empty document is a value, not a set of operators
        [InlineData("{ x : 1 }")]
        [InlineData("{ x : { $ne : 1 } }")] // only the top level element names are interpreted as operators
        [InlineData("{ x : 1, $ne : 2 }")] // only the first element name is interpreted as an operator
        [InlineData("[{ $ne : 1 }]")] // only a document at the top level could be an operator document
        [InlineData("'$ne'")] // a string is always a literal value, even when it looks like an operator name
        [InlineData("1")]
        [InlineData("null")]
        public void CanRepresent_should_return_true_for_a_literal_value(string valueAsJson)
        {
            var value = ParseValue(valueAsJson);

            var result = AstImpliedOperationFilterOperation.CanRepresent(value);

            result.Should().BeTrue();
        }

        [Fact]
        public void CanRepresent_should_return_true_for_a_regular_expression()
        {
            // a regex renders as { field : /pattern/options }, which is a regex match and not an injected operator
            var value = new BsonRegularExpression("^abc", "i");

            var result = AstImpliedOperationFilterOperation.CanRepresent(value);

            result.Should().BeTrue();
        }

        [Fact]
        public void constructor_should_throw_when_value_would_be_interpreted_as_query_operators()
        {
            var value = BsonDocument.Parse("{ $ne : 1 }");

            var exception = Record.Exception(() => new AstImpliedOperationFilterOperation(value));

            exception.Should().BeOfType<ArgumentException>()
                .Subject.ParamName.Should().Be("value");
        }

        [Fact]
        public void constructor_should_not_throw_when_value_is_a_literal_document()
        {
            var value = BsonDocument.Parse("{ x : 1 }");

            var result = new AstImpliedOperationFilterOperation(value);

            result.Value.Should().Be(value);
        }

        [Fact]
        public void constructor_should_throw_when_value_is_null()
        {
            var exception = Record.Exception(() => new AstImpliedOperationFilterOperation(null));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        // BsonDocument.Parse only parses documents, so wrap the value to parse any BSON type
        private static BsonValue ParseValue(string valueAsJson) => BsonDocument.Parse($"{{ v : {valueAsJson} }}")["v"];
    }
}
