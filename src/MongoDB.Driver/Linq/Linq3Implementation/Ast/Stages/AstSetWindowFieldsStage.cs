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
    internal sealed class AstSetWindowFieldsStage : AstStage
    {
        private readonly IReadOnlyList<AstWindowField> _output;
        private readonly AstExpression _partitionBy;
        private readonly AstSortFields _sortBy;

        public AstSetWindowFieldsStage(
            AstExpression partitionBy,
            AstSortFields sortBy,
            IEnumerable<AstWindowField> output)
        {
            _partitionBy = Ensure.IsNotNull(partitionBy, nameof(partitionBy));
            _sortBy = sortBy;
            _output = Ensure.IsNotNull(output, nameof(output)).AsReadOnlyList();
        }

        public override AstNodeType NodeType => AstNodeType.SetWindowFieldsStage;
        public IReadOnlyList<AstWindowField> Output => _output;
        public AstExpression PartitionBy => _partitionBy;
        public AstSortFields SortBy => _sortBy;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitSetWindowFieldsStage(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$setWindowFields", new BsonDocument
                    {
                        { "partitionBy", _partitionBy.Render() },
                        { "sortBy", _sortBy?.Render(), _sortBy != null },
                        { "output", new BsonDocument(_output.Select(f => f.RenderAsElement())) }
                    }
                }
            };
        }

        public AstSetWindowFieldsStage Update(
            AstExpression partitionBy,
            AstSortFields sortBy,
            IEnumerable<AstWindowField> output)
        {
            if (partitionBy == _partitionBy && sortBy == _sortBy && output == _output)
            {
                return this;
            }

            return new AstSetWindowFieldsStage(partitionBy, sortBy, output);
        }
    }
}
