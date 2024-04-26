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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class ParameterExpressionToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, ParameterExpression expression)
        {
            if (expression.Type == typeof(bool))
            {
                if (context.SymbolTable.TryGetSymbol(expression, out var symbol))
                {
                    var serializer = context.KnownSerializersRegistry.GetSerializer(expression);
                    var field = AstFilter.Field(symbol.Name, serializer);
                    var serializedTrue = SerializationHelper.SerializeValue(field.Serializer, true);
                    return AstFilter.Eq(field, serializedTrue);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
