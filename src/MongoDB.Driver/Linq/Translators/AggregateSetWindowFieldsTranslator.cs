/* Copyright 2021-present MongoDB Inc.
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
* 
*/

using System;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Processors;

namespace MongoDB.Driver.Linq.Translators
{
    internal static class AggregateSetWindowFieldsTranslator
    {
        public static RenderedProjectionDefinition<TResult> Translate<TPartitionBy, TDocument, TResult>(
            Expression<Func<TDocument, TPartitionBy>> partitionByProjector,
            SortDefinition<TDocument> sortDefinition,
            Expression<Func<IGrouping<TPartitionBy, TDocument>, TResult>> outputProjector,
            AggregateOutputWindowOptionsBase<TResult>[] outputWindowOptions,
            IBsonSerializer<TDocument> parameterSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            var bindingContext = new PipelineBindingContext(serializerRegistry);

            var keySelector = AggregateGroupTranslator.BindKeySelector(bindingContext, partitionByProjector, parameterSerializer);

            var partitionBy = AggregateLanguageTranslator.Translate(keySelector, translationOptions: null);

            var boundGroupExpression = AggregateGroupTranslator.BindGroup(bindingContext, outputProjector, parameterSerializer, keySelector);

            var projectionSerializer = bindingContext.GetSerializer(boundGroupExpression.Type, boundGroupExpression);
            var outputDocument = AggregateLanguageTranslator.Translate(boundGroupExpression, translationOptions: null).AsBsonDocument;
            var formattedOutputDocument = FormatOutputDocumentIfRequired(outputDocument);

            if (outputWindowOptions != null && outputWindowOptions.Length > 0)
            {
                foreach (var windowOptions in outputWindowOptions)
                {
                    var outputOptionFieldName = windowOptions.OutputWindowField.Render((IBsonSerializer<TResult>)projectionSerializer, serializerRegistry).FieldName;
                    if (formattedOutputDocument.TryGetValue(outputOptionFieldName, out var outputWindow) && outputWindow is BsonDocument outputWindowDocument)
                    {
                        var documents = windowOptions.Documents;
                        var range = windowOptions.Range;
                        var unit = windowOptions.Unit;
                        var outputWindowOptionsDocument = new BsonDocument
                        {
                            { "documents", () => ConvertWindowRangeIntoBsonArray(documents), documents != null },
                            { "range", () => ConvertWindowRangeIntoBsonArray(range), range != null },
                            { "unit", () => unit.ToString().ToLowerInvariant(), unit.HasValue }
                        };
                        outputWindowDocument.Add("window", outputWindowOptionsDocument);
                    }
                    else
                    {
                        throw new ArgumentException("The provided output is not a document or configured window options don't match to the provided pipeline.");
                    }
                }
            }

            var setWindowFieldsBody = new BsonDocument
            {
                { "partitionBy", partitionBy },
                { "sortBy", () => sortDefinition.Render(parameterSerializer, serializerRegistry), sortDefinition != null },
                { "output", formattedOutputDocument }
            };

            return new RenderedProjectionDefinition<TResult>(setWindowFieldsBody, (IBsonSerializer<TResult>)projectionSerializer);

            BsonArray ConvertWindowRangeIntoBsonArray(WindowRange range) => new BsonArray { range.Left.Value, range.Right.Value };
        }

        private static BsonDocument FormatOutputDocumentIfRequired(BsonDocument source)
        {
            var resultedDocument = new BsonDocument();
            FillResultedDocument(fieldNamePrefix: null, source, resultedDocument);
            return resultedDocument;
        }

        public static void FillResultedDocument(string fieldNamePrefix, BsonDocument source, BsonDocument resulted)
        {
            foreach (BsonElement item in source)
            {
                var fieldName = fieldNamePrefix != null ? $"{fieldNamePrefix}.{item.Name}" : item.Name;
                if (IsChildOnWindowLevel(item))
                {
                    resulted.Add(fieldName, item.Value);
                }
                else
                {
                    if (item.Value is BsonDocument itemValueDocument)
                    {
                        FillResultedDocument(fieldName, itemValueDocument, resulted);
                    }
                    else
                    {
                        // should not be reached
                        throw new ArgumentException($"The content {item.Value} of the output pipeline must be document.");
                    }
                }
            }
        }

        private static bool IsChildOnWindowLevel(BsonElement sourceElement)
        {
            if (sourceElement != null &&
                sourceElement.Value is BsonDocument childWindowLevel &&
                childWindowLevel.ElementCount == 1 &&
                childWindowLevel.GetElement(0).Name.StartsWith("$"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
