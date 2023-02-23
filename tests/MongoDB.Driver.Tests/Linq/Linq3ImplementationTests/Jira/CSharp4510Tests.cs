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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4510Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Expression_Func_C_object_with_nested_property_should_work([Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection();
            Expression<Func<C, object>> expression = x => x.SubDocument.NestedProperty;

            var fieldName = GetFieldName(collection, expression, linqProvider);
            fieldName.Should().Be("sub_document.nested_property");
        }

        [Theory]
        [ParameterAttributeData]
        public void Expression_Func_C_object_with_positional_operator_should_work([Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection();
#pragma warning disable CS0251 // Indexing an array with a negative index
            Expression<Func<C, object>> expression = linqProvider == LinqProvider.V2 ?
                x => x.ArrayField[-1] :
                x => x.ArrayField.FirstMatchingElement();
#pragma warning restore CS0251 // Indexing an array with a negative index

            var fieldName = GetFieldName(collection, expression, linqProvider);
            fieldName.Should().Be("array_field.$");
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("C");
            return collection;
        }

        private static string GetFieldName<TDocument>(IMongoCollection<TDocument> collection, Expression<Func<TDocument, object>> expression, LinqProvider linqProvider)
        {
            var fieldDefinition = new ExpressionFieldDefinition<TDocument, object>(expression);
            var renderedField = fieldDefinition.Render(collection.DocumentSerializer, BsonSerializer.SerializerRegistry, linqProvider);
            return renderedField.FieldName;
        }

        private class C
        {
            public int Id { get; set; }
            [BsonElement("array_field")] public int[] ArrayField { get; set; }
            [BsonElement("sub_document")] public S SubDocument { get; set; }
        }

        public class S
        {
            [BsonElement("nested_property")] public int NestedProperty { get; set; }
        }
    }
}
