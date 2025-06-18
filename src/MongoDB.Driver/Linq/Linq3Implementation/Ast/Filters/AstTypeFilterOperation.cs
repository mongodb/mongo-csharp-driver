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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters
{
    internal sealed class AstTypeFilterOperation : AstFilterOperation
    {
        private readonly IReadOnlyList<BsonType> _types;

        public AstTypeFilterOperation(BsonType type)
        {
            _types = new List<BsonType> { type }.AsReadOnly();
        }

        public AstTypeFilterOperation(IEnumerable<BsonType> types)
        {
            _types = Ensure.IsNotNull(types, nameof(types)).AsReadOnlyList();
        }

        public override AstNodeType NodeType => AstNodeType.TypeFilterOperation;
        public BsonType Type => _types.Single();
        public IReadOnlyList<BsonType> Types => _types;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitTypeFilterOperation(this);
        }

        public override BsonValue Render()
        {
            if (_types.Count == 1)
            {
                var type = _types[0];
                return new BsonDocument("$type", type.ToStringRepresentation());
            }
            else
            {
                return new BsonDocument("$type", new BsonArray(_types.Select(type => type.ToStringRepresentation())));
            }
        }
    }
}
