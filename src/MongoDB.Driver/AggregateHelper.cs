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
using System.Linq;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    internal static class AggregateHelper
    {
        public static RenderedPipelineDefinition<TResult> RenderAggregatePipeline<TDocument, TResult>(PipelineDefinition<TDocument, TResult> pipeline, RenderArgs<TDocument> renderArgs, out bool isAggregateToCollection)
        {
            var renderedPipeline = pipeline.Render(renderArgs);

            var lastStage = renderedPipeline.Documents.LastOrDefault();
            var lastStageName = lastStage?.GetElement(0).Name;
            isAggregateToCollection = lastStageName == "$out" || lastStageName == "$merge";

            return renderedPipeline;
        }

        public static CollectionNamespace GetOutCollection(BsonDocument outStage, DatabaseNamespace defaultDatabaseNamespace)
        {
            var stageName = outStage.GetElement(0).Name;
            switch (stageName)
            {
                case "$out":
                    {
                        var outValue = outStage[0];
                        DatabaseNamespace outputDatabaseNamespace;
                        string outputCollectionName;
                        if (outValue.IsString)
                        {
                            outputDatabaseNamespace = defaultDatabaseNamespace;
                            outputCollectionName = outValue.AsString;
                        }
                        else
                        {
                            outputDatabaseNamespace = new DatabaseNamespace(outValue["db"].AsString);
                            outputCollectionName = outValue["coll"].AsString;
                        }
                        return new CollectionNamespace(outputDatabaseNamespace, outputCollectionName);
                    }
                case "$merge":
                    {
                        var mergeArguments = outStage[0];
                        DatabaseNamespace outputDatabaseNamespace;
                        string outputCollectionName;
                        if (mergeArguments.IsString)
                        {
                            outputDatabaseNamespace = defaultDatabaseNamespace;
                            outputCollectionName = mergeArguments.AsString;
                        }
                        else
                        {
                            var into = mergeArguments.AsBsonDocument["into"];
                            if (into.IsString)
                            {
                                outputDatabaseNamespace = defaultDatabaseNamespace;
                                outputCollectionName = into.AsString;
                            }
                            else
                            {
                                if (into.AsBsonDocument.Contains("db"))
                                {
                                    outputDatabaseNamespace = new DatabaseNamespace(into["db"].AsString);
                                }
                                else
                                {
                                    outputDatabaseNamespace = defaultDatabaseNamespace;
                                }
                                outputCollectionName = into["coll"].AsString;
                            }
                        }
                        return new CollectionNamespace(outputDatabaseNamespace, outputCollectionName);
                    }
                default:
                    throw new ArgumentException($"Unexpected stage name: {stageName}.");
            }
        }
    }
}

