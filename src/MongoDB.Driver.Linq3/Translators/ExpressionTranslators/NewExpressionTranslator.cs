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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class NewExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, NewExpression expression)
        {
            //var translation = new BsonDocument();
            var computedFields = new List<AstComputedField>();
            var classMapType = typeof(BsonClassMap<>).MakeGenericType(expression.Type);
            var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);

            for (var i = 0; i < expression.Members.Count; i++)
            {
                var member = expression.Members[i];
                var fieldExpression = expression.Arguments[i];
                var translatedField = ExpressionTranslator.Translate(context, fieldExpression);
                //translation.Add(member.Name, translatedField.Translation);
                computedFields.Add(new AstComputedField(member.Name, translatedField.Translation));
                var translatedFieldSerializer = translatedField.Serializer ?? BsonSerializer.LookupSerializer(fieldExpression.Type);
                classMap.MapProperty(member.Name).SetSerializer(translatedFieldSerializer);
            }
            var translation = new AstComputedDocumentExpression(computedFields);

            var constructorInfo = expression.Type.GetConstructors().Single();
            var constructorArgumentNames = expression.Members.Select(m => m.Name).ToArray();
            classMap.MapConstructor(constructorInfo, constructorArgumentNames);
            classMap.Freeze();

            var serializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(expression.Type);
            var serializer = (IBsonSerializer)Activator.CreateInstance(serializerType, classMap);

            return new TranslatedExpression(expression, translation, serializer);
        }
    }
}
