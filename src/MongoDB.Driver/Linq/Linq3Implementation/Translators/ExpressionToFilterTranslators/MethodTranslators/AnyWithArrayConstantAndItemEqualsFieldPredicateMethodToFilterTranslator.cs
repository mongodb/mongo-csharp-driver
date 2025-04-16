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

using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    internal static class AnyWithArrayConstantAndItemEqualsFieldPredicateMethodToFilterTranslator
    {
        private static readonly MethodInfo[] __anyWithPredicateMethods =
        [
            EnumerableMethod.AnyWithPredicate,
            QueryableMethod.AnyWithPredicate,
        ];

        public static bool CanTranslate(MethodCallExpression expression, out ConstantExpression arrayConstantExpression, out Expression fieldExpression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            // arrayConstant.Any(item => item == <field>)
            // arrayConstant.Any(item => <field> == item)
            if (method.IsOneOf(__anyWithPredicateMethods))
            {
                var sourceExpression = arguments[0];
                var predicateExpression = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);

                if (sourceExpression.Type.IsArray &&
                    sourceExpression is ConstantExpression constantExpression)
                {
                    arrayConstantExpression = constantExpression;

                    var parameter = predicateExpression.Parameters.Single();
                    var body = predicateExpression.Body;

                    if (IsItemEqualsFieldComparison(body, parameter, out fieldExpression))
                    {
                        return true;
                    }
                }
            }

            arrayConstantExpression = null;
            fieldExpression = null;
            return false;
        }

        public static AstFilter Translate(TranslationContext context, ConstantExpression arrayConstantExpression, Expression fieldExpression)
        {
            var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);
            var itemSerializer = fieldTranslation.Serializer;
            var values = (IEnumerable)arrayConstantExpression.Value;
            var serializedArrayValues = SerializationHelper.SerializeValues(itemSerializer, values);
            return AstFilter.In(fieldTranslation.Ast, serializedArrayValues);
        }

        private static bool IsItemEqualsFieldComparison(
            Expression expression,
            ParameterExpression parameter,
            out Expression fieldExpression)
        {
            if (expression is BinaryExpression binaryExpression &&
                binaryExpression.NodeType == ExpressionType.Equal)
            {
                var left = binaryExpression.Left;
                var right = binaryExpression.Right;

                if (left == parameter)
                {
                    fieldExpression = right; // defer to Translate to throw if fieldExpression can't be translated to a field
                    return true;
                }

                if (right == parameter)
                {
                    fieldExpression = left; // defer to Translate to throw if fieldExpression can't be translated to a field
                    return true;
                }
            }

            fieldExpression = null;
            return false;
        }
    }
}
