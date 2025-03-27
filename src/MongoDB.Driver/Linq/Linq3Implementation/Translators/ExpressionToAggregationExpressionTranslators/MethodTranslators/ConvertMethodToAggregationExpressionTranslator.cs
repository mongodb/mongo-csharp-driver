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
        private static readonly List<(MethodInfo[] Methods, IBsonSerializer Serializer, BsonType Type, int? FormatIndex, int? SubTypeIndex, int? ByteOrderIndex, int? OptionsIndex)> _methodMappings =
        [
            ([MqlMethod.ToBinDataFromString], BsonValueSerializer.Instance, BsonType.Binary, 2, 1, null, null),
            ([MqlMethod.ToBinDataFromInt, MqlMethod.ToBinDataFromLong, MqlMethod.ToBinDataFromDouble, MqlMethod.ToBinDataFromNullableInt, MqlMethod.ToBinDataFromNullableLong, MqlMethod.ToBinDataFromNullableDouble],
                BsonValueSerializer.Instance, BsonType.Binary, null, 1, 2, null),
            ([MqlMethod.ToBinDataFromStringWithOptions], BsonValueSerializer.Instance, BsonType.Binary, 2, 1, null, 3),
            ([MqlMethod.ToBinDataFromIntWithOptions, MqlMethod.ToBinDataFromLongWithOptions, MqlMethod.ToBinDataFromDoubleWithOptions,
                    MqlMethod.ToBinDataFromNullableIntWithOptions, MqlMethod.ToBinDataFromNullableLongWithOptions, MqlMethod.ToBinDataFromNullableDoubleWithOptions],
                BsonValueSerializer.Instance, BsonType.Binary, null, 1, 2, 3),

            ([MqlMethod.ToDoubleFromBinData], DoubleSerializer.Instance, BsonType.Double, null, null, 1, null),
            ([MqlMethod.ToDoubleFromBinDataWithOptions], DoubleSerializer.Instance, BsonType.Double, null, null, 1, 2),
            ([MqlMethod.ToIntFromBinData], Int32Serializer.Instance, BsonType.Int32, null, null, 1, null),
            ([MqlMethod.ToIntFromBinDataWithOptions], Int32Serializer.Instance, BsonType.Int32, null, null, 1, 2),
            ([MqlMethod.ToLongFromBinData], Int64Serializer.Instance, BsonType.Int64, null, null, 1, null),
            ([MqlMethod.ToLongFromBinDataWithOptions], Int64Serializer.Instance, BsonType.Int64, null, null, 1, 2),

            ([MqlMethod.ToNullableDoubleFromBinData], new NullableSerializer<double>(DoubleSerializer.Instance), BsonType.Double, null, null, 1, null),
            ([MqlMethod.ToNullableDoubleFromBinDataWithOptions], new NullableSerializer<double>(DoubleSerializer.Instance), BsonType.Double, null, null, 1, 2),
            ([MqlMethod.ToNullableIntFromBinData], new NullableSerializer<int>(Int32Serializer.Instance), BsonType.Int32, null, null, 1, null),
            ([MqlMethod.ToNullableIntFromBinDataWithOptions], new NullableSerializer<int>(Int32Serializer.Instance), BsonType.Int32, null, null, 1, 2),
            ([MqlMethod.ToNullableLongFromBinData], new NullableSerializer<long>(Int64Serializer.Instance), BsonType.Int64, null, null, 1, null),
            ([MqlMethod.ToNullableLongFromBinDataWithOptions], new NullableSerializer<long>(Int64Serializer.Instance), BsonType.Int64, null, null, 1, 2),
            ([MqlMethod.ToStringFromBinData], StringSerializer.Instance, BsonType.String, 1, null, null, null),
            ([MqlMethod.ToStringFromBinDataWithOptions], StringSerializer.Instance, BsonType.String, 1, null, null, 2),
        ];

        private static readonly List<MethodInfo> ToStringMethods =
            [MqlMethod.ToStringFromBinData, MqlMethod.ToStringFromBinDataWithOptions];

        private static readonly List<MethodInfo> ToNumericalMethodsWithOptions =
            [MqlMethod.ToIntFromBinDataWithOptions, MqlMethod.ToLongFromBinDataWithOptions, MqlMethod.ToDoubleFromBinDataWithOptions];

        public static bool IsConvertToStringMethod(MethodInfo method)
        {
            return ToStringMethods.Contains(method);
        }

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;
            
            var mapping = _methodMappings.FirstOrDefault(m => m.Methods.Contains(method));
            if (mapping == default)
                throw new ExpressionNotSupportedException(expression);

            ByteOrder? byteOrder = null;
            BsonBinarySubType? subType = null;
            string format = null;
            ConvertOptions options = null;
            AstExpression onErrorAst = null;
            AstExpression onNullAst = null;

            var fieldAst = ExpressionToAggregationExpressionTranslator.Translate(context, arguments[0]).Ast;

            if (mapping.ByteOrderIndex.HasValue)
            {
                if (arguments[mapping.ByteOrderIndex.Value] is ConstantExpression co)
                {
                    byteOrder = (ByteOrder)co.Value!;
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

            if (mapping.OptionsIndex.HasValue)
            {
                if (arguments[mapping.OptionsIndex.Value] is ConstantExpression co)
                {
                    options = (ConvertOptions)co.Value!;

                    if (options == null)
                    {
                        throw new InvalidOperationException("The 'options' argument cannot be null");
                    }

                    if (ToNumericalMethodsWithOptions.Contains(method) && !options.OnNullWasSet)
                    {
                        throw new InvalidOperationException("When converting to a non-nullable type, you need to set 'onNull'");
                    }

                    if (options.OnErrorWasSet)
                    {
                        onErrorAst = options.GetOnError();
                    }

                    if (options.OnNullWasSet)
                    {
                        onNullAst = options.GetOnNull();
                    }
                }
                else
                {
                    throw new InvalidOperationException("The 'options' argument must be a constant expression");
                }
            }

            var ast = AstExpression.Convert(fieldAst, mapping.Type.Render(), subType: subType, byteOrder: byteOrder, format: format, onError: onErrorAst, onNull: onNullAst);
            return new TranslatedExpression(expression, ast, mapping.Serializer);
        }
    }
}
