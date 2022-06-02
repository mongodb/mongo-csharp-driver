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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages
{
    internal sealed class AstDensifyStage : AstStage
    {
        private readonly string _fieldPath;
        private readonly IReadOnlyList<string> _partitionByFieldPaths;
        private readonly DensifyRange _range;

        public AstDensifyStage(
            string fieldPath,
            DensifyRange range,
            IEnumerable<string> partitionByFieldPaths = null)
        {
            _fieldPath = Ensure.IsNotNull(fieldPath, nameof(fieldPath));
            _range = Ensure.IsNotNull(range, nameof(range));
            _partitionByFieldPaths = partitionByFieldPaths?.AsReadOnlyList();
        }

        public string FieldPath => _fieldPath;
        public override AstNodeType NodeType => AstNodeType.DensifyStage;
        public IReadOnlyList<string> PartitionByFieldPaths => _partitionByFieldPaths;
        public DensifyRange Range => _range;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitDensifyStage(this);
        }

        public override BsonValue Render()
        {
            var renderedArguments = new BsonDocument
            {
                { "field", _fieldPath },
                { "partitionByFields", () => new BsonArray(_partitionByFieldPaths), _partitionByFieldPaths?.Count > 0 },
                { "range", _range.Render() }
            };
            return new BsonDocument("$densify", renderedArguments);
        }
    }
}
