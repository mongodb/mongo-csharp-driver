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
using MongoDB.Driver.Linq3.Ast.Expressions;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class UnaryExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, UnaryExpression expression)
        {
            AstUnaryOperator? @operator = null;
            switch (expression.NodeType)
            {
                case ExpressionType.Convert: return TranslateConvert(context, expression);
                case ExpressionType.Not: @operator = AstUnaryOperator.Not; break;
            }

            if (@operator != null)
            {
                var translatedOperand = ExpressionTranslator.Translate(context, expression.Operand);

                //var translation = new BsonDocument(@operator, new BsonArray { translatedOperand.Translation });
                var translation = new AstUnaryExpression(@operator.Value, translatedOperand.Translation);
                return new TranslatedExpression(expression, translation, null);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static TranslatedExpression TranslateConvert(TranslationContext context, UnaryExpression expression)
        {
            string to;
            switch (expression.Type.FullName)
            {
                case "MongoDB.Bson.ObjectId": to = "objectId"; break;
                case "System.Boolean": to = "bool"; break;
                case "System.DateTime": to = "date"; break;
                case "System.Decimal": to = "decimal"; break;
                case "System.Double": to = "double"; break;
                case "System.Int32": to = "int"; break;
                case "System.Int64": to = "long"; break;
                case "System.String": to = "string"; break;
                default: throw new ExpressionNotSupportedException(expression);
            }

            var translatedOperand = ExpressionTranslator.Translate(context, expression.Operand);

            var translation = new AstConvertExpression(translatedOperand.Translation, to);
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer(expression.Type); // TODO: find correct serializer

            return new TranslatedExpression(expression, translation, serializer);
        }
    }
}
