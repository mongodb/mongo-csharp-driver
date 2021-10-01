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
    internal sealed class AstGroupStage : AstStage
    {
        private readonly IReadOnlyList<AstAccumulatorField> _fields;
        private readonly AstExpression _id;

        public AstGroupStage(
            AstExpression id,
            IEnumerable<AstAccumulatorField> fields)
        {
            _id = Ensure.IsNotNull(id, nameof(id));
            _fields = Ensure.IsNotNull(fields, nameof(fields)).AsReadOnlyList();
            Ensure.That(!_fields.Any(f => f.Path == "_id"), "An accumulator field of a $group stage cannot be named \"_id\".", nameof(fields));
        }

        public IReadOnlyList<AstAccumulatorField> Fields => _fields;
        public AstExpression Id => _id;
        public override AstNodeType NodeType => AstNodeType.GroupStage;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitGroupStage(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument("$group", new BsonDocument("_id", _id.Render()).AddRange(_fields.Select(f => f.RenderAsElement())));
        }

        public AstGroupStage Update(
            AstExpression id,
            IEnumerable<AstAccumulatorField> fields)
        {
            if (id == _id && fields == _fields)
            {
                return this;
            }

            return new AstGroupStage(id, fields);
        }
    }
}
