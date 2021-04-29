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
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq3.Jira
{
    public class CSharp1585Tests
    {
        [Fact]
        public void Nested_Any_should_translate_correctly()
        {
            var expression = (Expression<Func<Document, bool>>)(document => document.Details.A.Any(x => x.Any(y => Regex.IsMatch(y.DeviceName, @".Name0."))));
            var parameter = expression.Parameters[0];
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<Document>();
            var symbol = new Symbol("$CURRENT", documentSerializer);
            var symbolTable = new SymbolTable().WithSymbolAsCurrent(parameter, symbol);
            var context = new TranslationContext(symbolTable);
            var filter = ExpressionToFilterTranslator.Translate(context, expression.Body, exprOk: false);

            var rendered = filter.Render();

            rendered.Should().Be("{ 'Details.A' : { $elemMatch : { $elemMatch : { DeviceName : /.Name0./ } } } }");
        }

        [Fact]
        public void AstFilter_should_handle_nested_elemMatch()
        {
            var ast = AstFilter.ElemMatch(
                new AstFilterField("Details.A", BsonValueSerializer.Instance),
                AstFilter.ElemMatch(
                    new AstFilterField("$elem", BsonValueSerializer.Instance),
                    AstFilter.Regex(new AstFilterField("DeviceName", BsonValueSerializer.Instance), ".Name0.", "")));

            var rendered = ast.Render();

            rendered.Should().Be("{ 'Details.A' : { $elemMatch : { $elemMatch : { DeviceName : /.Name0./ } } } }");
        }

        // nested types
        public class Document
        {
            public int Id { get; set; }
            public Details Details { get; set; }
        }

        public class Details
        {
            public Device[][] A;
        }

        public class Device
        {
            public string DeviceName { get; set; }
        }
    }
}
