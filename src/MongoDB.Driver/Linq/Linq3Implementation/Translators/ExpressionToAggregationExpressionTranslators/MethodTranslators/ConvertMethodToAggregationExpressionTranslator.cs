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

            var fieldAst = ExpressionToAggregationExpressionTranslator.Translate(context, arguments[0]).Ast;

            AstExpression onErrorAst = null;
            AstExpression onNullAst = null;
            BsonBinarySubType? subType = null;
            ByteOrder? byteOrder = null;
            string format = null;


            var optionExpression = arguments[1];

            if (optionExpression is ConstantExpression constantExpression)
            {
                var options = (ConvertOptions)constantExpression.Value;

                if (options.OnErrorWasSet)
                {
                    onErrorAst = options.GetOnError();
                }

                if (options.OnNullWasSet)
                {
                    onNullAst = options.GetOnNull();
                }

                subType = options.SubType;
                format = options.Format;
                byteOrder = options.ByteOrder;
            }
            else if (arguments[1] is MemberInitExpression memberInitExpression)
            {
                foreach (var binding in memberInitExpression.Bindings)
                {
                    if (binding is not MemberAssignment memberAssignment) continue;

                    var memberName = memberAssignment.Member.Name;


                    if (memberName == "OnError")
                    {
                        var translatedExpression =
                            ExpressionToAggregationExpressionTranslator.Translate(context, memberAssignment.Expression);
                        onErrorAst = translatedExpression.Ast;
                    }
                    else if (memberName == "OnNull")
                    {
                        var translatedExpression =
                            ExpressionToAggregationExpressionTranslator.Translate(context, memberAssignment.Expression);
                        onNullAst = translatedExpression.Ast;
                    }
                    else if (memberName == "Format")
                    {
                        if (memberAssignment.Expression is not ConstantExpression formatExpression)
                        {
                            throw new ExpressionNotSupportedException(expression);  //TODO Improve
                        }

                        format = (string)formatExpression.Value;
                    }
                    else if (memberName == "ByteOrder")
                    {
                        if (memberAssignment.Expression is not ConstantExpression byteOrderExpression)
                        {
                            throw new ExpressionNotSupportedException(expression);  //TODO Improve
                        }

                        byteOrder = (ByteOrder)byteOrderExpression.Value;
                    }
                    else if (memberName == "SubType")
                    {
                        if (memberAssignment.Expression is not ConstantExpression subTypeExpression)
                        {
                            throw new ExpressionNotSupportedException(expression);  //TODO Improve
                        }

                        subType = (BsonBinarySubType)subTypeExpression.Value;
                    }
                }
            }

            var toType = method.GetGenericArguments()[1];
            var toBsonType = GetBsonType(toType).Render();
            var serializer = BsonSerializer.LookupSerializer(toType);

            var ast = AstExpression.Convert(fieldAst, toBsonType, subType: subType, byteOrder: byteOrder, format: format, onError: onErrorAst, onNull: onNullAst);
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
