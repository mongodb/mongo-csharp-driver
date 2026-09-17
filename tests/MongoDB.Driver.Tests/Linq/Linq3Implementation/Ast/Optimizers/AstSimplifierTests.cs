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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Optimizers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Ast.Optimizers
{
    public class AstSimplifierTests
    {
        [Theory]
        [MemberData(nameof(OperatorLikeDocuments))]
        public void Eq_with_operator_like_document_should_not_be_simplified(string valueAsJson)
        {
            var value = ParseValue(valueAsJson);
            var ast = AstFilter.Eq(new AstFilterField("X"), value);

            var rendered = AstSimplifier.Simplify(ast).Render();

            rendered.Should().Be(new BsonDocument("X", new BsonDocument("$eq", value)));
        }

        [Theory]
        [MemberData(nameof(OperatorLikeDocuments))]
        public void ElemMatch_eq_with_operator_like_document_should_not_be_simplified(string valueAsJson)
        {
            var value = ParseValue(valueAsJson);
            var ast = AstFilter.ElemMatch(
                new AstFilterField("Xs"),
                AstFilter.Eq(new AstFilterField("@<elem>"), value));

            var rendered = AstSimplifier.Simplify(ast).Render();

            rendered.Should().Be(new BsonDocument("Xs", new BsonDocument("$elemMatch", new BsonDocument("$eq", value))));
        }

        // a $regex document is not in OperatorLikeDocuments because the JSON parser reads { $regex : '...' }
        // as a regex literal, so it has to be built directly
        [Fact]
        public void Eq_with_regex_operator_document_should_not_be_simplified()
        {
            var value = new BsonDocument { { "$regex", "^secret" }, { "$options", "i" } };
            var ast = AstFilter.Eq(new AstFilterField("X"), value);

            var rendered = AstSimplifier.Simplify(ast).Render();

            rendered.Should().Be(new BsonDocument("X", new BsonDocument("$eq", value)));
        }

        [Fact]
        public void ElemMatch_eq_with_regex_operator_document_should_not_be_simplified()
        {
            var value = new BsonDocument { { "$regex", "^secret" }, { "$options", "i" } };
            var ast = AstFilter.ElemMatch(
                new AstFilterField("Xs"),
                AstFilter.Eq(new AstFilterField("@<elem>"), value));

            var rendered = AstSimplifier.Simplify(ast).Render();

            rendered.Should().Be(new BsonDocument("Xs", new BsonDocument("$elemMatch", new BsonDocument("$eq", value))));
        }

        [Theory]
        [MemberData(nameof(LiteralValues))]
        public void Eq_with_literal_value_should_be_simplified(string valueAsJson)
        {
            var value = ParseValue(valueAsJson);
            var ast = AstFilter.Eq(new AstFilterField("X"), value);

            var rendered = AstSimplifier.Simplify(ast).Render();

            rendered.Should().Be(new BsonDocument("X", value));
        }

        [Theory]
        [MemberData(nameof(LiteralValues))]
        public void ElemMatch_eq_with_literal_value_should_be_simplified(string valueAsJson)
        {
            var value = ParseValue(valueAsJson);
            var ast = AstFilter.ElemMatch(
                new AstFilterField("Xs"),
                AstFilter.Eq(new AstFilterField("@<elem>"), value));

            var rendered = AstSimplifier.Simplify(ast).Render();

            rendered.Should().Be(new BsonDocument("Xs", value));
        }

        [Fact]
        public void Eq_with_regex_value_should_not_be_simplified()
        {
            var value = new BsonRegularExpression("^abc", "i");
            var ast = AstFilter.Eq(new AstFilterField("X"), value);

            var rendered = AstSimplifier.Simplify(ast).Render();

            // a regex as an implied operation would be a regex match instead of an equality comparison
            rendered.Should().Be(new BsonDocument("X", new BsonDocument("$eq", value)));
        }

        [Fact]
        public void ElemMatch_eq_with_regex_value_should_not_be_simplified()
        {
            var value = new BsonRegularExpression("^abc", "i");
            var ast = AstFilter.ElemMatch(
                new AstFilterField("Xs"),
                AstFilter.Eq(new AstFilterField("@<elem>"), value));

            var rendered = AstSimplifier.Simplify(ast).Render();

            rendered.Should().Be(new BsonDocument("Xs", new BsonDocument("$elemMatch", new BsonDocument("$eq", value))));
        }

        // documents the server would interpret as operator documents if they appeared as the value of an implied
        // operation, turning a value supplied by the caller into query operators
        public static object[][] OperatorLikeDocuments =>
        [
            ["{ $ne : 1 }"],
            ["{ $gt : 1 }"],
            ["{ $lt : 1 }"],
            ["{ $in : [1, 2] }"],
            ["{ $exists : true }"],
            ["{ $ne : 1, x : 2 }"]
        ];

        // values the server always interprets literally and which therefore can safely be simplified
        public static object[][] LiteralValues =>
        [
            ["1"],
            ["0"],
            ["-1"],
            ["NumberLong(1)"],
            ["1.5"],
            ["true"],
            ["false"],
            ["'abc'"],
            ["''"],
            ["'$ne'"], // a string is always a literal value, even when it looks like an operator name
            ["null"],
            ["ObjectId('0102030405060708090a0b0c')"],
            ["ISODate('2026-08-14T00:00:00Z')"],
            ["BinData(0, 'AQID')"],
            ["[]"],
            ["[1, 2, 3]"],
            ["[{ $ne : 1 }]"], // only a document at the top level could be an operator document
            ["{ }"],
            ["{ x : 1 }"],
            ["{ x : { $ne : 1 } }"], // only the top level keys are interpreted as operators
            ["{ x : 1, $ne : 2 }"] // only the first key determines whether it is an operator document
        ];

        // parses a value of any BSON type by wrapping it in a document, since BsonDocument.Parse only parses documents
        private static BsonValue ParseValue(string valueAsJson) => BsonDocument.Parse($"{{ v : {valueAsJson} }}")["v"];
    }
}
