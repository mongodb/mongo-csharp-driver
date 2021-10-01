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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast
{
    internal sealed class AstPipeline : AstNode
    {
        #region static
        public static AstPipeline Empty(IBsonSerializer outputSerializer)
        {
            return new AstPipeline(Enumerable.Empty<AstStage>(), outputSerializer);
        }
        #endregion

        private IBsonSerializer _outputSerializer;
        private readonly IReadOnlyList<AstStage> _stages;

        public AstPipeline(IEnumerable<AstStage> stages, IBsonSerializer outputSerializer)
        {
            _stages = Ensure.IsNotNull(stages, nameof(stages)).AsReadOnlyList();
            _outputSerializer = Ensure.IsNotNull(outputSerializer, nameof(outputSerializer));
        }

        public override AstNodeType NodeType => AstNodeType.Pipeline;
        public IBsonSerializer OutputSerializer => _outputSerializer;
        public IReadOnlyList<AstStage> Stages => _stages;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitPipeline(this);
        }

        public AstPipeline AddStages(
            IBsonSerializer newOutputSerializer,
            params AstStage[] newStages)
        {
            var stages = _stages.Concat(newStages);
            return new AstPipeline(stages, newOutputSerializer);
        }

        public override BsonValue Render()
        {
            return new BsonArray(_stages.Select(s => s.Render()));
        }

        public AstPipeline ReplaceLastStage(
            IBsonSerializer newOutputSerializer,
            AstStage newLastStage)
        {
            var stages = _stages.Take(_stages.Count - 1).Concat(new[] { newLastStage });
            return new AstPipeline(stages, newOutputSerializer);
        }

        public AstPipeline Update(IEnumerable<AstStage> stages)
        {
            if (stages == _stages)
            {
                return this;
            }

            return new AstPipeline(stages, _outputSerializer);
        }
    }
}
