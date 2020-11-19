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
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq3.Misc
{
    public static class FieldResolver
    {
        // public methods
        public static ResolvedField ResolveField(Expression expression, SymbolTable symbolTable)
        {
            if (expression is ParameterExpression parameterExpression)
            {
                if (symbolTable.TryGetSymbol(parameterExpression, out Symbol symbol))
                {
                    var dottedFieldName = symbol == symbolTable.Current ? "$$CURRENT" : symbol.Name;
                    var fieldSerializer = symbol.Serializer;

                    if (fieldSerializer is IWrappedValueSerializer wrappedValueSerializer)
                    {
                        dottedFieldName = Combine(dottedFieldName, "_v");
                        fieldSerializer = wrappedValueSerializer.ValueSerializer;
                    }

                    return new ResolvedField(expression, dottedFieldName, fieldSerializer);
                }
            }
            else if (expression is MemberExpression memberExpression)
            {
                var containingField = ResolveField(memberExpression.Expression, symbolTable);
                if (containingField.Serializer is IBsonDocumentSerializer documentSerializer)
                {
                    if (documentSerializer.TryGetMemberSerializationInfo(memberExpression.Member.Name, out BsonSerializationInfo fieldSerializationInfo))
                    {
                        var fieldName = fieldSerializationInfo.ElementName;
                        var dottedFieldName = Combine(containingField.DottedFieldName, fieldName);
                        return new ResolvedField(expression, dottedFieldName, fieldSerializationInfo.Serializer);
                    }
                }
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                if (unaryExpression.NodeType == ExpressionType.Convert)
                {
                    var convertedField = ResolveField(unaryExpression.Operand, symbolTable);
                    var fieldType = convertedField.Serializer.ValueType;
                    if (fieldType.IsEnum)
                    {
                        var enumType = fieldType;
                        var enumUnderlyingType = enumType.GetEnumUnderlyingType();
                        if (unaryExpression.Type == enumUnderlyingType)
                        {
                            var enumAsUnderlyingTypeSerializer = EnumAsUnderlyingTypeSerializer.Create(convertedField.Serializer);
                            return new ResolvedField(expression, convertedField.DottedFieldName, enumAsUnderlyingTypeSerializer);
                        }
                    }
                }
            }
            else if (expression is MethodCallExpression methodCallExpression)
            {
                var method = methodCallExpression.Method;
                var arguments = methodCallExpression.Arguments;
                var parameters = method.GetParameters();

                if (method.IsStatic &&
                    method.Name == "First" &&
                    parameters.Length == 1)
                {
                    var containingField = ResolveField(arguments[0], symbolTable);
                    var dottedFieldName = Combine(containingField.DottedFieldName, "0");
                    var itemSerializer = ArraySerializerHelper.GetItemSerializer(containingField.Serializer);
                    if (method.ReturnType == itemSerializer.ValueType)
                    {
                        return new ResolvedField(expression, dottedFieldName, itemSerializer);
                    }
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private methods
        private static string Combine(string containingField, string member)
        {
            Throw.IfNullOrEmpty(containingField, nameof(containingField));
            Throw.IfNullOrEmpty(member, nameof(member));

            if (containingField == "$$CURRENT")
            {
                return member;
            }
            else
            {
                return containingField + "." + member;
            }
        }
    }
}
