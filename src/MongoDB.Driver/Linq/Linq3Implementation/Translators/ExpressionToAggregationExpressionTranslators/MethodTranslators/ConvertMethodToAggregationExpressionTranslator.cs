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
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal class ConvertMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (!method.Is(MqlMethod.Convert))
            {
                throw new ExpressionNotSupportedException(expression);
            }

            AstExpression onErrorAst = null;
            AstExpression onNullAst = null;

            var fieldAst = ExpressionToAggregationExpressionTranslator.Translate(context, arguments[0]).Ast;

            if (arguments[1] is not ConstantExpression constantExpression)
            {
                throw new InvalidOperationException("The 'options' argument must be a constant expression");
            }

            var options = (ConvertOptions)constantExpression.Value;

            if (options.OnErrorWasSet)
            {
                onErrorAst = options.GetOnError();
            }

            if (options.OnNullWasSet)
            {
                onNullAst = options.GetOnNull();
            }

            var toType = method.GetGenericArguments()[1];
            var toBsonType = GetBsonType(toType).Render();
            var serializer = BsonSerializer.LookupSerializer(toType);

            var ast = AstExpression.Convert(fieldAst, toBsonType, subType: options.SubType, byteOrder: options.ByteOrder, format: options.Format, onError: onErrorAst, onNull: onNullAst);
            return new TranslatedExpression(expression, ast, serializer);
        }

        public static BsonType GetBsonType(Type type)  //TODO Do we have this kind of info somewhere else...?
        {
            return Type.GetTypeCode(Nullable.GetUnderlyingType(type) ?? type) switch
            {
                TypeCode.Boolean => BsonType.Boolean,
                TypeCode.Byte => BsonType.Int32,
                TypeCode.SByte => BsonType.Int32,
                TypeCode.Int16 => BsonType.Int32,
                TypeCode.UInt16 => BsonType.Int32,
                TypeCode.Int32 => BsonType.Int32,
                TypeCode.UInt32 => BsonType.Int64,
                TypeCode.Int64 => BsonType.Int64,
                TypeCode.UInt64 => BsonType.Decimal128,
                TypeCode.Single => BsonType.Double,
                TypeCode.Double => BsonType.Double,
                TypeCode.Decimal => BsonType.Decimal128,
                TypeCode.String => BsonType.String,
                TypeCode.Char => BsonType.String,
                TypeCode.DateTime => BsonType.DateTime,
                TypeCode.Object when type == typeof(byte[]) => BsonType.Binary,
                TypeCode.Object when type == typeof(BsonBinaryData) => BsonType.Binary,
                _ when type == typeof(Guid) => BsonType.Binary,
                _ when type == typeof(ObjectId) => BsonType.ObjectId,
                _ => BsonType.Document
            };
        }
    }
}
