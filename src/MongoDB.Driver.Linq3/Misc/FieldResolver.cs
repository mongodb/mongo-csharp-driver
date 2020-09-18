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
    public class FieldResolver
    {
        // private fields
        private readonly SymbolTable _symbolTable;

        // constructors
        public FieldResolver(SymbolTable symbolTable)
        {
            _symbolTable = Throw.IfNull(symbolTable, nameof(symbolTable));
        }

        // public methods
        public bool TryResolveField(Expression expression, out ResolvedField resolvedField)
        {
            resolvedField = null;

            if (expression is ParameterExpression parameterExpression)
            {
                if (_symbolTable.TryGetSymbol(parameterExpression, out Symbol symbol))
                {
                    var dottedFieldName = symbol.Name;
                    var fieldSerializer = symbol.Serializer;

                    if (fieldSerializer is IWrappedValueSerializer wrappedValueSerializer)
                    {
                        dottedFieldName = Combine(dottedFieldName, "_v");
                        fieldSerializer = wrappedValueSerializer.ValueSerializer;
                    }

                    resolvedField = new ResolvedField(expression, dottedFieldName, fieldSerializer);
                    return true;
                }
            }

            if (expression is MemberExpression memberExpression)
            {
                if (TryResolveField(memberExpression.Expression, out ResolvedField containingField))
                {
                    if (containingField.Serializer is IBsonDocumentSerializer documentSerializer)
                    {
                        if (documentSerializer.TryGetMemberSerializationInfo(memberExpression.Member.Name, out BsonSerializationInfo fieldSerializationInfo))
                        {
                            var fieldName = fieldSerializationInfo.ElementName;
                            var dottedFieldName = Combine(containingField.DottedFieldName, fieldName);
                            resolvedField = new ResolvedField(expression, dottedFieldName, fieldSerializationInfo.Serializer);
                            return true;
                        }
                    }
                }
            }

            if (expression is UnaryExpression unaryExpression)
            {
                if (unaryExpression.NodeType == ExpressionType.Convert)
                {
                    if (TryResolveField(unaryExpression.Operand, out ResolvedField convertedField))
                    {
                        var fieldType = convertedField.Serializer.ValueType;
                        if (fieldType.IsEnum)
                        {
                            var enumType = fieldType;
                            var enumUnderlyingType = enumType.GetEnumUnderlyingType();
                            if (unaryExpression.Type == enumUnderlyingType)
                            {
                                var enumAsUnderlyingTypeSerializer = EnumAsUnderlyingTypeSerializer.Create(convertedField.Serializer);
                                resolvedField = new ResolvedField(expression, convertedField.DottedFieldName, enumAsUnderlyingTypeSerializer);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        // private methods
        private string Combine(string containingField, string member)
        {
            Throw.IfNullOrEmpty(containingField, nameof(containingField));
            Throw.IfNullOrEmpty(member, nameof(member));

            if (containingField == "$CURRENT")
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
