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

using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal class ConvertMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __toBinDataMethods =
        {
            MqlMethod.ToBinDataFromString,
            MqlMethod.ToBinDataFromInt,
            MqlMethod.ToBinDataFromLong,
            MqlMethod.ToBinDataFromDouble
        };

        private static readonly MethodInfo[] __withOnErrorAndOnNullMethods =
        {
            MqlMethod.ToBinDataFromStringWithOnErrorAndOnNull,
            MqlMethod.ToBinDataFromIntWithOnErrorAndOnNull,
            MqlMethod.ToBinDataFromLongWithOnErrorAndOnNull,
            MqlMethod.ToBinDataFromDoubleWithOnErrorAndOnNull
        };

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (!method.IsOneOf(__toBinDataMethods, __withOnErrorAndOnNullMethods))
            {
                throw new ExpressionNotSupportedException(expression);
            }

            var fieldExpression = arguments[0];
            var fieldTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, fieldExpression);
            var fieldAst = fieldTranslation.Ast;
            var resultSerializer = BsonBinaryDataSerializer.Instance;

            var subTypeExpression = arguments[1];
            var subTypeTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, subTypeExpression);
            var subTypeAst = subTypeTranslation.Ast;

            var formatExpression = arguments[2];
            var formatTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, formatExpression);
            var formatAst = formatTranslation.Ast;

            if (method.IsOneOf(__withOnErrorAndOnNullMethods))
            {
                var onErrorExpression = arguments[3];
                var onErrorTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, onErrorExpression);
                var onErrorAst = onErrorTranslation.Ast;

                //TODO Continue
            }

            var ast = AstExpression.Convert(fieldAst, AstExpression.Constant(BsonType.Binary), subType: subTypeAst, format: formatAst);
            return new TranslatedExpression(expression, ast, resultSerializer);
        }
    }
}