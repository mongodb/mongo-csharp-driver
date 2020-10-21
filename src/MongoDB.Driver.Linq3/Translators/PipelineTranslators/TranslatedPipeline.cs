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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Stages;

namespace MongoDB.Driver.Linq3.Translators.PipelineTranslators
{
    public class TranslatedPipeline
    {
        // private fields
        private IBsonSerializer _outputSerializer;
        private readonly List<AstPipelineStage> _stages;

        // constructors
        public TranslatedPipeline(IBsonSerializer inputSerializer)
        {
            _stages = new List<AstPipelineStage>();
            _outputSerializer = inputSerializer;
        }

        // public properties
        public IBsonSerializer OutputSerializer => _outputSerializer;

        public IReadOnlyList<AstPipelineStage> Stages => _stages.AsReadOnly();

        // public methods
        public void AddStages(
            IBsonSerializer newOutputSerializer,
            params AstPipelineStage[] stages)
        {
            _stages.AddRange(stages);
            _outputSerializer = newOutputSerializer;
        }

        public void ReplaceLastStage(
            IBsonSerializer newOutputSerializer,
            AstPipelineStage newLastStage)
        {
            _stages[_stages.Count - 1] = newLastStage;
            _outputSerializer = newOutputSerializer;
        }

        public BsonDocumentStagePipelineDefinition<TInput, TOutput> ToPipelineDefinition<TInput, TOutput>()
        {
            var renderedStages = _stages.Select(s => s.Render().AsBsonDocument);
            return new BsonDocumentStagePipelineDefinition<TInput, TOutput>(renderedStages, (IBsonSerializer<TOutput>)_outputSerializer);
        }

        public override string ToString()
        {
            return new BsonArray(_stages.Select(s => s.Render())).ToJson();
        }
    }
}
