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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages
{
    internal sealed class AstLookupWithPipelineStage : AstStage
    {
        private readonly string _as;
        private readonly string _from;
        private readonly IReadOnlyList<AstComputedField> _let;
        private readonly AstPipeline _pipeline;

        public AstLookupWithPipelineStage(
            string from,
            IEnumerable<AstComputedField> let,
            AstPipeline pipeline,
            string @as)
        {
            _from = Ensure.IsNotNull(from, nameof(from));
            _let = let?.AsReadOnlyList(); // can be null for an uncorrelated subquery
            _pipeline = Ensure.IsNotNull(pipeline, nameof(pipeline));
            _as = Ensure.IsNotNull(@as, nameof(@as));
        }

        public string As => _as;
        public string From => _from;
        public IReadOnlyList<AstComputedField> Let => _let;
        public override AstNodeType NodeType => AstNodeType.LookupWithPipelineStage;
        public AstPipeline Pipeline => _pipeline;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitLookupWithPipelineStage(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$lookup", new BsonDocument()
                    {
                        { "from", _from },
                        { "let", () => new BsonDocument(_let.Select(l => l.RenderAsElement())), _let?.Count > 0 },
                        { "pipeline", _pipeline.Render() },
                        { "as", _as }
                    }
                }
            };
        }

        public AstLookupWithPipelineStage Update(IReadOnlyList<AstComputedField> let, AstPipeline pipeline)
        {
            if (let == _let && pipeline == _pipeline)
            {
                return this;
            }

            return new AstLookupWithPipelineStage(_from, let, pipeline, _as);
        }
    }
}
