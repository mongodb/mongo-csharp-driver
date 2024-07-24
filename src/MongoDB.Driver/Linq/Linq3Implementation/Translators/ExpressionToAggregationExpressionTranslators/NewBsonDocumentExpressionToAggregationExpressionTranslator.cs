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
using System.Linq.Expressions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class NewBsonDocumentExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, NewExpression expression)
        {
            return Translate(context, expression, newExpression: expression, initializers: Array.Empty<(Expression, Expression)>());
        }

        public static AggregationExpression Translate(TranslationContext context, ListInitExpression expression)
        {
            var initializers = new List<(Expression, Expression)>();
            foreach (var initializer in expression.Initializers)
            {
                if (!initializer.AddMethod.Is(BsonDocumentMethod.AddWithNameAndValue))
                {
                    throw new ExpressionNotSupportedException(expression, because: "it uses an unsupported Add method");
                }

                var nameExpression = initializer.Arguments[0];
                var valueExpresssion = initializer.Arguments[1];

                initializers.Add((nameExpression, valueExpresssion));
            }

            return Translate(context, expression, expression.NewExpression, initializers);
        }

        public static AggregationExpression Translate(TranslationContext context, MemberInitExpression expression)
        {
            if (expression.Bindings.Count > 0)
            {
                throw new ExpressionNotSupportedException(expression, because: "it uses an unsupported initializer syntax");
            }

            return Translate(context, expression, expression.NewExpression, initializers: Array.Empty<(Expression, Expression)>());
        }

        private static AggregationExpression Translate(
            TranslationContext context,
            Expression expression,
            NewExpression newExpression,
            IEnumerable<(Expression FieldName, Expression Value)> initializers)
        {
            var computedFields = new List<AstComputedField>();

            if (newExpression != null)
            {
                var constructorInfo = newExpression.Constructor;
                if (constructorInfo.Is(BsonDocumentConstructor.WithNoParameters))
                {
                    // nothing to do
                }
                else if (constructorInfo.Is(BsonDocumentConstructor.WithNameAndValue))
                {
                    var arguments = newExpression.Arguments;
                    var nameExpression = arguments[0];
                    var valueExpresssion = arguments[1];
                    computedFields.Add(CreateComputedField(context, expression, nameExpression, valueExpresssion));
                }
                else
                {
                    throw new ExpressionNotSupportedException(newExpression, expression, because: "it uses an unsupported constructor");
                }
            }

            foreach (var (nameExpression, valueExpression) in initializers)
            {
                computedFields.Add(CreateComputedField(context, expression, nameExpression, valueExpression));
            }

            var ast = AstExpression.ComputedDocument(computedFields);
            return new AggregationExpression(expression, ast, BsonDocumentSerializer.Instance);

            static AstComputedField CreateComputedField(TranslationContext context, Expression expression, Expression fieldNameExpression, Expression valueExpression)
            {
                var fieldName = fieldNameExpression.GetConstantValue<string>(expression);
                var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                return AstExpression.ComputedField(fieldName, valueTranslation.Ast);
            }
        }
    }
}
