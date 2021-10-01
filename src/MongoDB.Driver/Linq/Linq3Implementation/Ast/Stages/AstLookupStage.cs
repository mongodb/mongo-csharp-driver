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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages
{
    internal abstract class AstLookupStageMatch : AstNode
    {
        public override BsonValue Render()
        {
            return new BsonDocument(RenderAsElements());
        }

        public abstract IEnumerable<BsonElement> RenderAsElements();
    }

    internal sealed class AstLookupStageEqualityMatch : AstLookupStageMatch
    {
        private readonly string _foreignField;
        private readonly string _localField;

        public AstLookupStageEqualityMatch(string localField, string foreignField)
        {
            _localField = Ensure.IsNotNull(localField, nameof(localField));
            _foreignField = Ensure.IsNotNull(foreignField, nameof(foreignField));
        }

        public string ForeignField => _foreignField;
        public string LocalField => _localField;
        public override AstNodeType NodeType => AstNodeType.LookupStageEqualityMatch;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitLookupStageEqualityMatch(this);
        }

        public override IEnumerable<BsonElement> RenderAsElements()
        {
            return new BsonDocument
            {
                { "localField", _localField },
                { "foreignField", _foreignField }
            };
        }
    }

    internal sealed class AstLookupStageUncorrelatedMatch : AstLookupStageMatch
    {
        private readonly IReadOnlyList<AstComputedField> _let;
        private readonly AstPipeline _pipeline;

        public AstLookupStageUncorrelatedMatch(AstPipeline pipeline, IEnumerable<AstComputedField> let)
        {
            _pipeline = Ensure.IsNotNull(pipeline, nameof(pipeline));
            _let = let?.AsReadOnlyList();
        }

        public IReadOnlyList<AstComputedField> Let => _let;
        public override AstNodeType NodeType => AstNodeType.LookupStageUncorrelatedMatch;
        public AstPipeline Pipeline => _pipeline;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitLookupStageUncorrelatedMatch(this);
        }

        public override IEnumerable<BsonElement> RenderAsElements()
        {
            return new BsonDocument
            {
                { "let", () => new BsonDocument(_let.Select(l => l.RenderAsElement())), _let != null },
                { "pipeline", _pipeline.Render() }
            };
        }

        public AstLookupStageUncorrelatedMatch Update(AstPipeline pipeline, IEnumerable<AstComputedField> let)
        {
            if (pipeline == _pipeline && let == _let)
            {
                return this;
            }

            return new AstLookupStageUncorrelatedMatch(pipeline, let);
        }
    }

    internal sealed class AstLookupStage : AstStage
    {
        private readonly string _as;
        private readonly string _from;
        private readonly AstLookupStageMatch _match;

        public AstLookupStage(string from, AstLookupStageMatch match, string @as)
        {
            _from = Ensure.IsNotNull(from, nameof(from));
            _match = Ensure.IsNotNull(match, nameof(match));
            _as = Ensure.IsNotNull(@as, nameof(@as));
        }

        public string As => _as;
        public string From => _from;
        public new AstLookupStageMatch Match => _match;
        public override AstNodeType NodeType => AstNodeType.LookupStage;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitLookupStage(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$lookup", new BsonDocument()
                    .Add("from", _from)
                    .AddRange(_match.RenderAsElements())
                    .Add("as", _as)
                }
            };
        }

        public AstLookupStage Update(AstLookupStageMatch match)
        {
            if (match == _match)
            {
                return this;
            }

            return new AstLookupStage(_from, match, _as);
        }
    }
}
