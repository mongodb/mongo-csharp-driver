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
using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class PickExpressionDefinitionBuilderTests
    {
        [Fact]
        public void Top_with_lambda_should_render_expected_expression()
        {
            var sortBy = Builders<Item>.Sort.Descending(x => x.Score);

            var expression = PickExpressionDefinitionBuilder.Top<Document, Item>(
                x => x.Items,
                sortBy);

            var rendered = RenderExpression(expression);
            rendered.Should().Be("{ $top : { input : '$Items', sortBy : { Score : -1 } } }");
        }

        [Fact]
        public void Top_with_field_definition_should_render_expected_expression()
        {
            var sortBy = Builders<Item>.Sort.Descending(x => x.Score);

            var expression = PickExpressionDefinitionBuilder.Top<Document, Item>(
                new StringFieldDefinition<Document, IEnumerable<Item>>("Items"),
                sortBy);

            var rendered = RenderExpression(expression);
            rendered.Should().Be("{ $top : { input : '$Items', sortBy : { Score : -1 } } }");
        }

        [Fact]
        public void Top_with_compound_sort_should_render_expected_expression()
        {
            var sortBy = Builders<Item>.Sort.Descending(x => x.Score).Ascending(x => x.Name);

            var expression = PickExpressionDefinitionBuilder.Top<Document, Item>(x => x.Items, sortBy);

            var rendered = RenderExpression(expression);
            rendered.Should().Be("{ $top : { input : '$Items', sortBy : { Score : -1, Name : 1 } } }");
        }

        [Fact]
        public void Bottom_with_lambda_should_render_expected_expression()
        {
            var sortBy = Builders<Item>.Sort.Ascending(x => x.Score);

            var expression = PickExpressionDefinitionBuilder.Bottom<Document, Item>(
                x => x.Items,
                sortBy);

            var rendered = RenderExpression(expression);
            rendered.Should().Be("{ $bottom : { input : '$Items', sortBy : { Score : 1 } } }");
        }

        [Fact]
        public void Bottom_with_field_definition_should_render_expected_expression()
        {
            var sortBy = Builders<Item>.Sort.Ascending(x => x.Score);

            var expression = PickExpressionDefinitionBuilder.Bottom(
                new StringFieldDefinition<Document, IEnumerable<Item>>("Items"),
                sortBy);

            var rendered = RenderExpression(expression);
            rendered.Should().Be("{ $bottom : { input : '$Items', sortBy : { Score : 1 } } }");
        }

        [Fact]
        public void TopN_with_lambda_should_render_expected_expression()
        {
            var sortBy = Builders<Item>.Sort.Descending(x => x.Score);

            var expression = PickExpressionDefinitionBuilder.TopN<Document, Item>(
                x => x.Items,
                sortBy,
                n: 3);

            var rendered = RenderExpression(expression);
            rendered.Should().Be("{ $topN : { input : '$Items', sortBy : { Score : -1 }, n : 3 } }");
        }

        [Fact]
        public void TopN_with_field_definition_should_render_expected_expression()
        {
            var sortBy = Builders<Item>.Sort.Descending(x => x.Score);

            var expression = PickExpressionDefinitionBuilder.TopN(
                new StringFieldDefinition<Document, IEnumerable<Item>>("Items"),
                sortBy,
                n: 5);

            var rendered = RenderExpression(expression);
            rendered.Should().Be("{ $topN : { input : '$Items', sortBy : { Score : -1 }, n : 5 } }");
        }

        [Fact]
        public void TopN_with_n_equals_one_should_render_expected_expression()
        {
            var sortBy = Builders<Item>.Sort.Descending(x => x.Score);

            var expression = PickExpressionDefinitionBuilder.TopN<Document, Item>(x => x.Items, sortBy, n: 1);

            var rendered = RenderExpression(expression);
            rendered.Should().Be("{ $topN : { input : '$Items', sortBy : { Score : -1 }, n : 1 } }");
        }

        [Fact]
        public void TopN_with_invalid_n_should_throw()
        {
            var sortBy = Builders<Item>.Sort.Descending(x => x.Score);

            var exception = Record.Exception(() =>
                PickExpressionDefinitionBuilder.TopN<Document, Item>(x => x.Items, sortBy, n: 0));

            exception.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void BottomN_with_lambda_should_render_expected_expression()
        {
            var sortBy = Builders<Item>.Sort.Descending(x => x.Score);

            var expression = PickExpressionDefinitionBuilder.BottomN<Document, Item>(
                x => x.Items,
                sortBy,
                n: 3);

            var rendered = RenderExpression(expression);
            rendered.Should().Be("{ $bottomN : { input : '$Items', sortBy : { Score : -1 }, n : 3 } }");
        }

        [Fact]
        public void BottomN_with_field_definition_should_render_expected_expression()
        {
            var sortBy = Builders<Item>.Sort.Descending(x => x.Score);

            var expression = PickExpressionDefinitionBuilder.BottomN(
                new StringFieldDefinition<Document, IEnumerable<Item>>("Items"),
                sortBy,
                n: 2);

            var rendered = RenderExpression(expression);
            rendered.Should().Be("{ $bottomN : { input : '$Items', sortBy : { Score : -1 }, n : 2 } }");
        }

        [Fact]
        public void SetFieldDefinitionsBuilder_Set_with_aggregate_expression_using_field_definition_should_render_expected_stage()
        {
            var sortBy = Builders<Item>.Sort.Descending(x => x.Score);
            var pickExpr = PickExpressionDefinitionBuilder.TopN<Document, Item>(x => x.Items, sortBy, n: 3);

            var setDef = Builders<Document>.SetFields.Set(
                new StringFieldDefinition<Document, IEnumerable<Item>>("TopItems"),
                pickExpr);

            var rendered = RenderSetFieldDefinitions(setDef);
            rendered.Should().Be("{ TopItems : { $topN : { input : '$Items', sortBy : { Score : -1 }, n : 3 } } }");
        }

        [Fact]
        public void SetFieldDefinitionsBuilder_Set_with_aggregate_expression_using_lambda_should_render_expected_stage()
        {
            var sortBy = Builders<Item>.Sort.Descending(x => x.Score);
            var pickExpr = PickExpressionDefinitionBuilder.TopN<Document, Item>(x => x.Items, sortBy, n: 3);

            var setDef = Builders<Document>.SetFields.Set(
                x => x.TopItems,
                pickExpr);

            var rendered = RenderSetFieldDefinitions(setDef);
            rendered.Should().Be("{ TopItems : { $topN : { input : '$Items', sortBy : { Score : -1 }, n : 3 } } }");
        }

        [Fact]
        public void ListSetFieldDefinitionsExtensions_Set_with_aggregate_expression_should_chain_correctly()
        {
            var sortByDesc = Builders<Item>.Sort.Descending(x => x.Score);
            var sortByAsc = Builders<Item>.Sort.Ascending(x => x.Score);

            var setDef = Builders<Document>.SetFields
                .Set(x => x.TopItems, PickExpressionDefinitionBuilder.TopN<Document, Item>(x => x.Items, sortByDesc, n: 3))
                .Set(x => x.TopItems, PickExpressionDefinitionBuilder.BottomN<Document, Item>(x => x.Items, sortByAsc, n: 1));

            var rendered = RenderSetFieldDefinitions(setDef);
            // Last Set wins when same field is used
            rendered.Should().Be("{ TopItems : { $bottomN : { input : '$Items', sortBy : { Score : 1 }, n : 1 } } }");
        }

        [Fact]
        public void TopN_with_nested_field_path_should_render_dollar_prefix_correctly()
        {
            var sortBy = Builders<Item>.Sort.Descending(x => x.Score);

            var expression = PickExpressionDefinitionBuilder.TopN(
                new StringFieldDefinition<DocumentWithNested, IEnumerable<Item>>("Nested.Items"),
                sortBy,
                n: 2);

            var rendered = RenderExpression(expression);
            rendered.Should().Be("{ $topN : { input : '$Nested.Items', sortBy : { Score : -1 }, n : 2 } }");
        }

        private static BsonValue RenderExpression<TDocument, TResult>(AggregateExpressionDefinition<TDocument, TResult> expression)
        {
            var registry = BsonSerializer.SerializerRegistry;
            var serializer = registry.GetSerializer<TDocument>();
            return expression.Render(new RenderArgs<TDocument>(serializer, registry));
        }

        private static BsonDocument RenderSetFieldDefinitions<TDocument>(SetFieldDefinitions<TDocument> setDef)
        {
            var registry = BsonSerializer.SerializerRegistry;
            var serializer = registry.GetSerializer<TDocument>();
            return setDef.Render(new RenderArgs<TDocument>(serializer, registry));
        }

        private class Document
        {
            [BsonElement("Items")]
            public List<Item> Items { get; set; }

            [BsonElement("TopItems")]
            public List<Item> TopItems { get; set; }
        }

        private class DocumentWithNested
        {
            [BsonElement("Nested")]
            public NestedDocument Nested { get; set; }
        }

        private class NestedDocument
        {
            [BsonElement("Items")]
            public List<Item> Items { get; set; }
        }

        private class Item
        {
            [BsonElement("Score")]
            public int Score { get; set; }

            [BsonElement("Name")]
            public string Name { get; set; }
        }
    }
}
