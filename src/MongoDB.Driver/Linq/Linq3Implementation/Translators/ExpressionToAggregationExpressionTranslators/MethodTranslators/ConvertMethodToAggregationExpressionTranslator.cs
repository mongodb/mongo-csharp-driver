using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal class ConvertMethodToAggregationExpressionTranslator
    {
        private static readonly List<(MethodInfo[] Methods, IBsonSerializer Serializer, BsonType Type, int? FormatIndex, int? SubTypeIndex, int? ByteOrderIndex, int? OnErrorIndex, int? OnNullIndex)> _methodMappings =
        [
            ([MqlMethod.ConvertToBinDataFromString], BsonBinaryDataSerializer.Instance, BsonType.Binary, 2, 1, null, null, null),
            ([MqlMethod.ConvertToBinDataFromInt, MqlMethod.ConvertToBinDataFromLong, MqlMethod.ConvertToBinDataFromDouble],
                BsonBinaryDataSerializer.Instance, BsonType.Binary, null, 1, 2, null, null),
            ([MqlMethod.ConvertToBinDataFromStringWithOnErrorAndOnNull], BsonBinaryDataSerializer.Instance, BsonType.Binary, 2, 1, null, 3, 4),
            ([MqlMethod.ConvertToBinDataFromIntWithOnErrorAndOnNull, MqlMethod.ConvertToBinDataFromLongWithOnErrorAndOnNull, MqlMethod.ConvertToBinDataFromDoubleWithOnErrorAndOnNull],
                BsonBinaryDataSerializer.Instance, BsonType.Binary, null, 1, 2, 3, 4),
            ([MqlMethod.ConvertToStringFromBinData], StringSerializer.Instance, BsonType.String, 1, null, null, null, null),
            ([MqlMethod.ConvertToStringFromBinDataWithOnErrorAndOnNull], StringSerializer.Instance, BsonType.String, 1, null, null, 2, 3),
            ([MqlMethod.ConvertToNullableIntFromBinData], new NullableSerializer<int>(Int32Serializer.Instance), BsonType.Int32, null, null, 1, null, null),
            ([MqlMethod.ConvertToNullableIntFromBinDataWithOnErrorAndOnNull], new NullableSerializer<int>(Int32Serializer.Instance), BsonType.Int32, null, null, 1, 2, 3),
            ([MqlMethod.ConvertToNullableLongFromBinData], new NullableSerializer<long>(Int64Serializer.Instance), BsonType.Int64, null, null, 1, null, null),
            ([MqlMethod.ConvertToNullableLongFromBinDataWithOnErrorAndOnNull], new NullableSerializer<long>(Int64Serializer.Instance), BsonType.Int64, null, null, 1, 2, 3),
            ([MqlMethod.ConvertToNullableDoubleFromBinData], new NullableSerializer<double>(DoubleSerializer.Instance), BsonType.Double, null, null, 1, null, null),
            ([MqlMethod.ConvertToNullableDoubleFromBinDataWithOnErrorAndOnNull], new NullableSerializer<double>(DoubleSerializer.Instance), BsonType.Double, null, null, 1, 2, 3)
        ];
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;
            
            var mapping = _methodMappings.FirstOrDefault(m => m.Methods.Contains(method));
            if (mapping == default)
                throw new ExpressionNotSupportedException(expression);

            Mql.ByteOrder? byteOrder = null;
            BsonBinarySubType? subType = null;
            string format = null;

            var fieldAst = ExpressionToAggregationExpressionTranslator.Translate(context, arguments[0]).Ast;
            var subTypeAst = mapping.SubTypeIndex.HasValue ? ExpressionToAggregationExpressionTranslator.Translate(context, arguments[mapping.SubTypeIndex.Value]).Ast : null;
            var onErrorAst = mapping.OnErrorIndex.HasValue ? ExpressionToAggregationExpressionTranslator.Translate(context, arguments[mapping.OnErrorIndex.Value]).Ast : null;
            var onNullAst = mapping.OnNullIndex.HasValue ? ExpressionToAggregationExpressionTranslator.Translate(context, arguments[mapping.OnNullIndex.Value]).Ast : null;

            if (mapping.ByteOrderIndex.HasValue)
            {
                if (arguments[mapping.ByteOrderIndex.Value] is ConstantExpression co)
                {
                    byteOrder = (Mql.ByteOrder)co.Value!;
                }
                else
                {
                    throw new InvalidOperationException("The 'byteOrder' argument must be a constant expression");
                }
            }

            if (mapping.FormatIndex.HasValue)
            {
                if (arguments[mapping.FormatIndex.Value] is ConstantExpression co)
                {
                    format = (string)co.Value!;
                }
                else
                {
                    throw new InvalidOperationException("The 'format' argument must be a constant expression");
                }
            }

            if (mapping.SubTypeIndex.HasValue)
            {
                if (arguments[mapping.SubTypeIndex.Value] is ConstantExpression co)
                {
                    subType = (BsonBinarySubType)co.Value!;
                }
                else
                {
                    throw new InvalidOperationException("The 'subType' argument must be a constant expression");
                }
            }

            var ast = AstExpression.Convert(fieldAst, mapping.Type.Render(), onError: onErrorAst, onNull: onNullAst, subType: subType, format: format, byteOrder: byteOrder);
            return new TranslatedExpression(expression, ast, mapping.Serializer);
        }
    }
}
