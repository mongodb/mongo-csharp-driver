using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal class ConvertMethodToAggregationExpressionTranslator
    {
        private static readonly List<(MethodInfo[] Methods, IBsonSerializer Serializer, BsonType Type, int? FormatIndex, int? SubTypeIndex, int? OnErrorIndex, int? OnNullIndex)> _methodMappings =
        [
            ([MqlMethod.ConvertToBinDataFromString, MqlMethod.ConvertToBinDataFromInt, MqlMethod.ConvertToBinDataFromLong, MqlMethod.ConvertToBinDataFromDouble], BsonBinaryDataSerializer.Instance, BsonType.Binary, 2, 1, null, null),
            ([MqlMethod.ConvertToBinDataFromStringWithOnErrorAndOnNull, MqlMethod.ConvertToBinDataFromIntWithOnErrorAndOnNull, MqlMethod.ConvertToBinDataFromLongWithOnErrorAndOnNull, MqlMethod.ConvertToBinDataFromDoubleWithOnErrorAndOnNull], BsonBinaryDataSerializer.Instance, BsonType.Binary, 2, 1, 3, 4),
            ([MqlMethod.ConvertToStringFromBinData], StringSerializer.Instance, BsonType.String, 1, null, null, null),
            ([MqlMethod.ConvertToStringFromBinDataWithOnErrorAndOnNull], StringSerializer.Instance, BsonType.String, 1, null, 2, 3),
            ([MqlMethod.ConvertToIntFromBinData], new NullableSerializer<int>(Int32Serializer.Instance), BsonType.Int32, 1, null, null, null),
            ([MqlMethod.ConvertToIntFromBinDataWithOnErrorAndOnNull], new NullableSerializer<int>(Int32Serializer.Instance), BsonType.Int32, 1, null, 2, 3),
            ([MqlMethod.ConvertToLongFromBinData], new NullableSerializer<long>(Int64Serializer.Instance), BsonType.Int64, 1, null, null, null),
            ([MqlMethod.ConvertToLongFromBinDataWithOnErrorAndOnNull], new NullableSerializer<long>(Int64Serializer.Instance), BsonType.Int64, 1, null, 2, 3),
            ([MqlMethod.ConvertToDoubleFromBinData], new NullableSerializer<double>(DoubleSerializer.Instance), BsonType.Double, 1, null, null, null),
            ([MqlMethod.ConvertToDoubleFromBinDataWithOnErrorAndOnNull], new NullableSerializer<double>(DoubleSerializer.Instance), BsonType.Double, 1, null, 2, 3)
        ];
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;
            
            var mapping = _methodMappings.FirstOrDefault(m => m.Methods.Contains(method));
            if (mapping == default)
                throw new ExpressionNotSupportedException(expression);

            var fieldAst = ExpressionToAggregationExpressionTranslator.Translate(context, arguments[0]).Ast;
            var formatAst = mapping.FormatIndex.HasValue ? ExpressionToAggregationExpressionTranslator.Translate(context, arguments[mapping.FormatIndex.Value]).Ast : null;
            var subTypeAst = mapping.SubTypeIndex.HasValue ? ExpressionToAggregationExpressionTranslator.Translate(context, arguments[mapping.SubTypeIndex.Value]).Ast : null;
            var onErrorAst = mapping.OnErrorIndex.HasValue ? ExpressionToAggregationExpressionTranslator.Translate(context, arguments[mapping.OnErrorIndex.Value]).Ast : null;
            var onNullAst = mapping.OnNullIndex.HasValue ? ExpressionToAggregationExpressionTranslator.Translate(context, arguments[mapping.OnNullIndex.Value]).Ast : null;

            var ast = AstExpression.Convert(fieldAst, AstExpression.Constant(mapping.Type), onError: onErrorAst, onNull: onNullAst, subType: subTypeAst, format: formatAst);
            return new TranslatedExpression(expression, ast, mapping.Serializer);
        }
    }
}
