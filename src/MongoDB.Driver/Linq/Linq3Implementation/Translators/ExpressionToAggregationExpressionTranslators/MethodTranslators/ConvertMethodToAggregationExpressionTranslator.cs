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
        private static readonly MethodInfo[] __convertToBinDataMethods =
        [
            MqlMethod.ConvertToBinDataFromString,
            MqlMethod.ConvertToBinDataFromInt,
            MqlMethod.ConvertToBinDataFromLong,
            MqlMethod.ConvertToBinDataFromDouble
        ];

        private static readonly MethodInfo[] __convertToBinDataWithOnErrorAndOnNullMethods =
        [
            MqlMethod.ConvertToBinDataFromStringWithOnErrorAndOnNull,
            MqlMethod.ConvertToBinDataFromIntWithOnErrorAndOnNull,
            MqlMethod.ConvertToBinDataFromLongWithOnErrorAndOnNull,
            MqlMethod.ConvertToBinDataFromDoubleWithOnErrorAndOnNull
        ];

        private static readonly MethodInfo[] __convertToStringMethods =
        [
            MqlMethod.ConvertToStringFromBinData
        ];

        private static readonly MethodInfo[] __convertToStringWithOnErrorAndOnNullMethods =
        [
            MqlMethod.ConvertToStringFromBinDataWithOnErrorAndOnNull
        ];

        private static readonly MethodInfo[] __convertToIntMethods =
        [
            MqlMethod.ConvertToIntFromBinData
        ];

        private static readonly MethodInfo[] __convertToIntWithOnErrorAndOnNullMethods =
        [
            MqlMethod.ConvertToIntFromBinDataWithOnErrorAndOnNull
        ];

        private static readonly MethodInfo[] __convertToLongMethods =
        [
            MqlMethod.ConvertToLongFromBinData
        ];

        private static readonly MethodInfo[] __convertToLongWithOnErrorAndOnNullMethods =
        [
            MqlMethod.ConvertToLongFromBinDataWithOnErrorAndOnNull
        ];

        private static readonly MethodInfo[] __convertToDoubleMethods =
        [
            MqlMethod.ConvertToDoubleFromBinData
        ];

        private static readonly MethodInfo[] __convertToDoubleWithOnErrorAndOnNullMethods =
        [
            MqlMethod.ConvertToDoubleFromBinDataWithOnErrorAndOnNull
        ];

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (!method.IsOneOf(__convertToBinDataMethods, __convertToBinDataWithOnErrorAndOnNullMethods,
                    __convertToStringMethods, __convertToStringWithOnErrorAndOnNullMethods,
                    __convertToIntMethods, __convertToIntWithOnErrorAndOnNullMethods,
                    __convertToLongMethods, __convertToLongWithOnErrorAndOnNullMethods,
                    __convertToDoubleMethods, __convertToDoubleWithOnErrorAndOnNullMethods))
            {
                throw new ExpressionNotSupportedException(expression);
            }

            AstExpression fieldAst = null;
            AstExpression subTypeAst = null;
            AstExpression formatAst = null;
            AstExpression onErrorAst = null;
            AstExpression onNullAst = null;

            var subTypeIndex = -1;
            var formatIndex = -1;
            var onErrorIndex = -1;
            var onNullIndex = -1;

            var fieldExpression = arguments[0];
            var fieldTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, fieldExpression);
            fieldAst = fieldTranslation.Ast;

            if (method.IsOneOf(__convertToBinDataMethods, __convertToBinDataWithOnErrorAndOnNullMethods))
            {
                subTypeIndex = 1;
                formatIndex = 2;

                if (method.IsOneOf(__convertToBinDataWithOnErrorAndOnNullMethods))
                {
                    onErrorIndex = 3;
                    onNullIndex = 4;
                }
            }

            if (subTypeIndex > 0)
            {
                var subTypeExpression = arguments[subTypeIndex];
                var subTypeTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, subTypeExpression);
                subTypeAst = subTypeTranslation.Ast;
            }
            if (formatIndex > 0)
            {
                var formatExpression = arguments[formatIndex];
                var formatTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, formatExpression);
                formatAst = formatTranslation.Ast;
            }
            if (onErrorIndex > 0)
            {
                var onErrorExpression = arguments[onErrorIndex];
                var onErrorTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, onErrorExpression);
                onErrorAst = onErrorTranslation.Ast;
            }
            if (onNullIndex > 0)
            {
                var onNullExpression = arguments[onNullIndex];
                var onNullTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, onNullExpression);
                onNullAst = onNullTranslation.Ast;
            }


            var ast = AstExpression.Convert(fieldAst, AstExpression.Constant(BsonType.Binary), subType: subTypeAst, format: formatAst, onError: onErrorAst, onNull: onNullAst);
            return new TranslatedExpression(expression, ast, BsonBinaryDataSerializer.Instance);


        }
    }
}