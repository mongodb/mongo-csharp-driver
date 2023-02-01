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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages
{
    internal sealed class AstBucketStage : AstStage
    {
        private readonly IReadOnlyList<BsonValue> _boundaries;
        private readonly BsonValue _default;
        private readonly AstExpression _groupBy;
        private readonly IReadOnlyList<AstAccumulatorField> _output;

        public AstBucketStage(
            AstExpression groupBy,
            IEnumerable<BsonValue> boundaries,
            BsonValue @default = null,
            IEnumerable<AstAccumulatorField> output = null)
        {
            _groupBy = Ensure.IsNotNull(groupBy, nameof(groupBy));
            _boundaries = Ensure.IsNotNull(boundaries, nameof(boundaries)).AsReadOnlyList();
            _default = @default; // can be null
            _output = output?.AsReadOnlyList(); // can be null
        }

        public IReadOnlyList<BsonValue> Boundaries => _boundaries;
        public BsonValue Default => _default;
        public AstExpression GroupBy => _groupBy;
        public override AstNodeType NodeType => AstNodeType.BucketStage;
        public IReadOnlyList<AstAccumulatorField> Output => _output;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitBucketStage(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$bucket", new BsonDocument
                    {
                        { "groupBy", _groupBy.Render() },
                        { "boundaries", new BsonArray(_boundaries) },
                        { "default", _default, _default != null },
                        { "output", () => new BsonDocument(_output.Select(f => f.RenderAsElement())), _output != null }
                    }
                }
            };
        }

        public AstBucketStage Update(
            AstExpression groupBy,
            IEnumerable<AstAccumulatorField> output)
        {
            if (groupBy == _groupBy && output == _output)
            {
                return this;
            }

            return new AstBucketStage(groupBy, _boundaries, _default, output);
        }
    }
}
