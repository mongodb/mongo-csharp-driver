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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Translators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    public class ModuloComparisonExpressionToFilterTranslatorTests
    {
        [Fact]
        public void Translate_should_return_expected_result_with_byte_arguments()
        {
            var (parameter, expression) = CreateExpression((C c) => c.Byte % 2 == 1);
            var context = CreateContext(parameter);
            var canTranslate = ModuloComparisonExpressionToFilterTranslator.CanTranslate(expression.Left, expression.Right, out var moduloExpression, out var remainderExpression);
            canTranslate.Should().BeTrue();

            var result = ModuloComparisonExpressionToFilterTranslator.Translate(context, expression, moduloExpression, remainderExpression);

            Assert(result, "Byte", 2, 1);
        }

        [Fact]
        public void Translate_should_return_expected_result_with_decimal_arguments()
        {
            var (parameter, expression) = CreateExpression((C c) => c.Decimal % 2 == 1);
            var context = CreateContext(parameter);
            var canTranslate = ModuloComparisonExpressionToFilterTranslator.CanTranslate(expression.Left, expression.Right, out var moduloExpression, out var remainderExpression);
            canTranslate.Should().BeTrue();

            var result = ModuloComparisonExpressionToFilterTranslator.Translate(context, expression, moduloExpression, remainderExpression);

            Assert(result, "Decimal", 2M, 1M);
        }

        [Fact]
        public void Translate_should_return_expected_result_with_double_arguments()
        {
            var (parameter, expression) = CreateExpression((C c) => c.Double % 2 == 1);
            var context = CreateContext(parameter);
            var canTranslate = ModuloComparisonExpressionToFilterTranslator.CanTranslate(expression.Left, expression.Right, out var moduloExpression, out var remainderExpression);
            canTranslate.Should().BeTrue();

            var result = ModuloComparisonExpressionToFilterTranslator.Translate(context, expression, moduloExpression, remainderExpression);

            Assert(result, "Double", 2.0, 1.0);
        }

        [Fact]
        public void Translate_should_return_expected_result_with_float_arguments()
        {
            var (parameter, expression) = CreateExpression((C c) => c.Float % 2 == 1);
            var context = CreateContext(parameter);
            var canTranslate = ModuloComparisonExpressionToFilterTranslator.CanTranslate(expression.Left, expression.Right, out var moduloExpression, out var remainderExpression);
            canTranslate.Should().BeTrue();

            var result = ModuloComparisonExpressionToFilterTranslator.Translate(context, expression, moduloExpression, remainderExpression);

            Assert(result, "Float", 2.0F, 1.0F);
        }

        [Fact]
        public void Translate_should_return_expected_result_with_int16_arguments()
        {
            var (parameter, expression) = CreateExpression((C c) => c.Int16 % 2 == 1);
            var context = CreateContext(parameter);
            var canTranslate = ModuloComparisonExpressionToFilterTranslator.CanTranslate(expression.Left, expression.Right, out var moduloExpression, out var remainderExpression);
            canTranslate.Should().BeTrue();

            var result = ModuloComparisonExpressionToFilterTranslator.Translate(context, expression, moduloExpression, remainderExpression);

            Assert(result, "Int16", 2, 1);
        }

        [Fact]
        public void Translate_should_return_expected_result_with_int32_arguments()
        {
            var (parameter, expression) = CreateExpression((C c) => c.Int32 % 2 == 1);
            var context = CreateContext(parameter);
            var canTranslate = ModuloComparisonExpressionToFilterTranslator.CanTranslate(expression.Left, expression.Right, out var moduloExpression, out var remainderExpression);
            canTranslate.Should().BeTrue();

            var result = ModuloComparisonExpressionToFilterTranslator.Translate(context, expression, moduloExpression, remainderExpression);

            Assert(result, "Int32", 2, 1);
        }

        [Fact]
        public void Translate_should_return_expected_result_with_int64_arguments()
        {
            var (parameter, expression) = CreateExpression((C c) => c.Int64 % 2 == 1);
            var context = CreateContext(parameter);
            var canTranslate = ModuloComparisonExpressionToFilterTranslator.CanTranslate(expression.Left, expression.Right, out var moduloExpression, out var remainderExpression);
            canTranslate.Should().BeTrue();

            var result = ModuloComparisonExpressionToFilterTranslator.Translate(context, expression, moduloExpression, remainderExpression);

            Assert(result, "Int64", 2L, 1L);
        }

        [Fact]
        public void Translate_should_return_expected_result_with_sbyte_arguments()
        {
            var (parameter, expression) = CreateExpression((C c) => c.SByte % 2 == 1);
            var context = CreateContext(parameter);
            var canTranslate = ModuloComparisonExpressionToFilterTranslator.CanTranslate(expression.Left, expression.Right, out var moduloExpression, out var remainderExpression);
            canTranslate.Should().BeTrue();

            var result = ModuloComparisonExpressionToFilterTranslator.Translate(context, expression, moduloExpression, remainderExpression);

            Assert(result, "SByte", 2, 1);
        }

        [Fact]
        public void Translate_should_return_expected_result_with_uint16_arguments()
        {
            var (parameter, expression) = CreateExpression((C c) => c.UInt16 % 2 == 1);
            var context = CreateContext(parameter);
            var canTranslate = ModuloComparisonExpressionToFilterTranslator.CanTranslate(expression.Left, expression.Right, out var moduloExpression, out var remainderExpression);
            canTranslate.Should().BeTrue();

            var result = ModuloComparisonExpressionToFilterTranslator.Translate(context, expression, moduloExpression, remainderExpression);

            Assert(result, "UInt16", 2, 1);
        }

        [Fact]
        public void Translate_should_return_expected_result_with_uint32_arguments()
        {
            var (parameter, expression) = CreateExpression((C c) => c.UInt32 % 2 == 1);
            var context = CreateContext(parameter);
            var canTranslate = ModuloComparisonExpressionToFilterTranslator.CanTranslate(expression.Left, expression.Right, out var moduloExpression, out var remainderExpression);
            canTranslate.Should().BeTrue();

            var result = ModuloComparisonExpressionToFilterTranslator.Translate(context, expression, moduloExpression, remainderExpression);

            Assert(result, "UInt32", 2L, 1L);
        }

        [Fact]
        public void Translate_should_return_expected_result_with_uint64_arguments()
        {
            var (parameter, expression) = CreateExpression((C c) => c.UInt64 % 2 == 1);
            var context = CreateContext(parameter);
            var canTranslate = ModuloComparisonExpressionToFilterTranslator.CanTranslate(expression.Left, expression.Right, out var moduloExpression, out var remainderExpression);
            canTranslate.Should().BeTrue();

            var result = ModuloComparisonExpressionToFilterTranslator.Translate(context, expression, moduloExpression, remainderExpression);

            Assert(result, "UInt64", 2L, 1L);
        }

        private void Assert(AstFilter result, string path, BsonValue divisor, BsonValue remainder)
        {
            var fieldOperationFilter = result.Should().BeOfType<AstFieldOperationFilter>().Subject;
            fieldOperationFilter.Field.Path.Should().Be(path);
            var modFilterOperation = fieldOperationFilter.Operation.Should().BeOfType<AstModFilterOperation>().Subject;
            modFilterOperation.Divisor.Should().Be(divisor);
            modFilterOperation.Remainder.Should().Be(remainder);
        }

        private TranslationContext CreateContext(ParameterExpression parameter)
        {
            var domain = BsonSerializer.DefaultSerializationDomain;
            var serializer = domain.LookupSerializer(parameter.Type);
            var context = TranslationContext.Create(translationOptions: null, domain);
            var symbol = context.CreateSymbol(parameter, serializer, isCurrent: true);
            return context.WithSymbol(symbol);
        }

        private (ParameterExpression, BinaryExpression) CreateExpression<TField>(Expression<Func<TField, bool>> lambda)
        {
            var parameter = lambda.Parameters.Single();
            var expression = (BinaryExpression)lambda.Body;
            return (parameter, expression);
        }

        private class C
        {
            public byte Byte { get; set; }
            public decimal Decimal { get; set; }
            public double Double { get; set; }
            public float Float { get; set; }
            public short Int16 { get; set; }
            public int Int32 { get; set; }
            public long Int64 { get; set; }
            public sbyte SByte { get; set; }
            public ushort UInt16 { get; set; }
            public uint UInt32 { get; set; }
            public ulong UInt64 { get; set; }
        }
    }
}
