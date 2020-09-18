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
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class MemberExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MemberExpression expression)
        {
            var container = expression.Expression;
            var member = expression.Member;

            var translatedContainer = ExpressionTranslator.Translate(context, container);
            var fieldInfo = DocumentSerializerHelper.GetFieldInfo(translatedContainer.Serializer, member.Name);

            //if (translatedContainer.Translation.IsString && translatedContainer.Translation.AsString.StartsWith("$"))
            if (translatedContainer.Translation is AstFieldExpression fieldExpression)
            {
                //var containerTranslation = translatedContainer.Translation.AsString;
                //var translation = TranslatedFieldHelper.Combine(containerTranslation, fieldInfo.ElementName);
                var translation = new AstFieldExpression(TranslatedFieldHelper.Combine(fieldExpression.Field, fieldInfo.ElementName));
                return new TranslatedExpression(expression, translation, fieldInfo.Serializer);
            }
            else
            {
                //var translation = new BsonDocument("$let", new BsonDocument
                //{
                //    { "vars", new BsonDocument("_document", translatedContainer.Translation) },
                //    { "in", $"$$_document.{fieldInfo.ElementName}" }
                //});
                var translation = new AstLetExpression(
                    new[] { new AstComputedField("_document", translatedContainer.Translation) },
                    new AstFieldExpression($"$$_document.{fieldInfo.ElementName}"));
                return new TranslatedExpression(expression, translation, fieldInfo.Serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
