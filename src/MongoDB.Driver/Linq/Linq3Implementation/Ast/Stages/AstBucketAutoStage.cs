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
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages
{
    internal sealed class AstBucketAutoStage : AstStage
    {
        private readonly int _buckets;
        private readonly string _granularity;
        private readonly AstExpression _groupBy;
        private readonly IReadOnlyList<AstComputedField> _output;

        public AstBucketAutoStage(
            AstExpression groupBy,
            int buckets,
            string granularity = null,
            IEnumerable<AstComputedField> output = null)
        {
            _groupBy = Ensure.IsNotNull(groupBy, nameof(groupBy));
            _buckets = Ensure.IsGreaterThanZero(buckets, nameof(buckets));
            _granularity = granularity; // can be null
            _output = output?.ToList().AsReadOnly(); // can be null
        }

        public int Buckets => _buckets;
        public string Granularity => _granularity;
        public AstExpression GroupBy => _groupBy;
        public override AstNodeType NodeType => AstNodeType.BucketAutoStage;
        public IReadOnlyList<AstComputedField> Output => _output;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$group", new BsonDocument
                    {
                        { "groupBy", _groupBy.Render() },
                        { "buckets", _buckets },
                        { "output", () => new BsonDocument(_output.Select(f => f.Render())), _output != null },
                        { "granularity", _granularity, _granularity != null }
                    }
                }
            };
        }
    }
}
