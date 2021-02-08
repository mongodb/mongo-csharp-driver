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
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Serializers;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators
{
    public static class ExpressionToFilterFieldTranslator
    {
        // public static methods
        public static AstFilterField Translate(TranslationContext context, Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.ArrayIndex: return ArrayIndexExpressionToFilterFieldTranslator.Translate(context, (BinaryExpression)expression);
                case ExpressionType.MemberAccess: return MemberExpressionToFilterFieldTranslator.Translate(context, (MemberExpression)expression);
                case ExpressionType.Call: return MethodCallExpressionToFilterFieldTranslator.Translate(context, (MethodCallExpression)expression);
                case ExpressionType.Parameter: return ParameterExpressionToFilterFieldTranslator.Translate(context, (ParameterExpression)expression);
                case ExpressionType.Convert: return ConvertExpressionToFilterFieldTranslator.Translate(context, (UnaryExpression)expression);
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
    }
}
