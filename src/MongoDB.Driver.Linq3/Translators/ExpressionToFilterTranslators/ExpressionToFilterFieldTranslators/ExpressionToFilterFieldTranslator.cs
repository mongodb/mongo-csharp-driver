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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ExpressionToFilterFieldTranslators
{
    public static class ExpressionToFilterFieldTranslator
    {
        // public static methods
        public static AstFilterField Translate(TranslationContext context, Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.MemberAccess: return TranslateMemberExpression(context, (MemberExpression)expression);
                case ExpressionType.Call: return TranslateMethodCallExpression(context, (MethodCallExpression)expression);
                case ExpressionType.Parameter: return TranslateParameterExpression(context, (ParameterExpression)expression);
                case ExpressionType.Convert: return TranslateConvertExpression(context, (UnaryExpression)expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        public static AstFilterField TranslateEnumerable(TranslationContext context, Expression expression)
        {
            var resolvedFieldAst = Translate(context, expression);

            var serializer = resolvedFieldAst.Serializer;
            if (serializer is IWrappedEnumerableSerializer wrappedEnumerableSerializer)
            {
                var enumerableSerializer = IEnumerableSerializer.Create(wrappedEnumerableSerializer.EnumerableElementSerializer);
                resolvedFieldAst = resolvedFieldAst.CreateFilterSubField(wrappedEnumerableSerializer.EnumerableFieldName, enumerableSerializer);
            }

            return resolvedFieldAst;
        }

        // private static methods
        private static AstFilterField TranslateMemberExpression(TranslationContext context, MemberExpression memberExpression)
        {
            var containingFieldAst = Translate(context, memberExpression.Expression);
            if (containingFieldAst.Serializer is IBsonDocumentSerializer documentSerializer &&
                documentSerializer.TryGetMemberSerializationInfo(memberExpression.Member.Name, out BsonSerializationInfo memberSerializationInfo))
            {
                var fieldName = memberSerializationInfo.ElementName;
                var fieldSerializer = memberSerializationInfo.Serializer;
                return containingFieldAst.CreateFilterSubField(fieldName, fieldSerializer);
            }

            throw new ExpressionNotSupportedException(memberExpression);
        }

        private static AstFilterField TranslateMethodCallExpression(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;
            var parameters = method.GetParameters();

            if (method.IsStatic &&
                method.Name == "First" &&
                parameters.Length == 1)
            {
                var enumerableFieldAst = TranslateEnumerable(context, arguments[0]);
                if (enumerableFieldAst.Serializer is IBsonArraySerializer arraySerializer &&
                    arraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo))
                {
                    var itemSerializer = itemSerializationInfo.Serializer;
                    if (method.ReturnType.IsAssignableFrom(itemSerializer.ValueType))
                    {
                        return enumerableFieldAst.CreateFilterSubField("0", itemSerializer);
                    }
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstFilterField TranslateParameterExpression(TranslationContext context, ParameterExpression expression)
        {
            var symbolTable = context.SymbolTable;
            if (symbolTable.TryGetSymbol(expression, out Symbol symbol))
            {
                var fieldName = symbol == symbolTable.Current ? "$CURRENT" : symbol.Name;
                var fieldSerializer = symbol.Serializer;
                var fieldAst = new AstFilterField(fieldName, fieldSerializer);

                if (fieldSerializer is IWrappedValueSerializer wrappedValueSerializer)
                {
                    fieldAst = fieldAst.CreateFilterSubField("_v", wrappedValueSerializer.ValueSerializer);
                }

                return fieldAst; ;
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstFilterField TranslateConvertExpression(TranslationContext context, UnaryExpression expression)
        {
            var convertedFieldAst = Translate(context, expression.Operand);
            var fieldType = convertedFieldAst.Serializer.ValueType;
            if (fieldType.IsEnum)
            {
                var enumType = fieldType;
                var enumUnderlyingType = enumType.GetEnumUnderlyingType();
                if (expression.Type == enumUnderlyingType)
                {
                    var enumAsUnderlyingTypeSerializer = EnumAsUnderlyingTypeSerializer.Create(convertedFieldAst.Serializer);
                    return new AstFilterField(convertedFieldAst.Path, enumAsUnderlyingTypeSerializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
