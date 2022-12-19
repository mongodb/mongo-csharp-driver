﻿/* Copyright 2010-present MongoDB Inc.
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

using System.Collections;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    internal static class DocumentsMethodToPipelineTranslator
    {
        // public static methods
        public static AstPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(MongoQueryableMethod.Documents, MongoQueryableMethod.DocumentsWithSerializer))
            {
                var sourceExpression = ConvertHelper.RemoveConvertToMongoQueryable(arguments[0]);
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);

                var documentsExpression = arguments[1];
                var documents = documentsExpression.GetConstantValue<IEnumerable>(expression);

                IBsonSerializer documentSerializer;
                if (method.Is(MongoQueryableMethod.DocumentsWithSerializer))
                {
                    var documentSerializerExpression = arguments[2];
                    documentSerializer = documentSerializerExpression.GetConstantValue<IBsonSerializer>(expression);
                }
                else
                {
                    var documentType = method.GetGenericArguments()[0];
                    documentSerializer = BsonSerializer.LookupSerializer(documentType);
                }

                var serializedDocuments = SerializationHelper.SerializeValues(documentSerializer, documents);
                var documentsStage = AstStage.Documents(serializedDocuments);
                pipeline = pipeline.AddStages(documentSerializer, documentsStage);

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
