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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages
{
    internal sealed class AstLookupStage : AstStage
    {
        private readonly string _as;
        private readonly string _foreignField;
        private readonly string _from;
        private readonly string _localField;

        public AstLookupStage(
            string from,
            string localField,
            string foreignField,
            string @as)
        {
            _from = Ensure.IsNotNull(from, nameof(from));
            _localField = Ensure.IsNotNull(localField, nameof(localField));
            _foreignField = Ensure.IsNotNull(foreignField, nameof(foreignField));
            _as = Ensure.IsNotNull(@as, nameof(@as));
        }

        public string As => _as;
        public string ForeignField => _foreignField;
        public string From => _from;
        public string LocalField => _localField;
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
                    {
                        { "from", _from },
                        { "localField", _localField },
                        { "foreignField", _foreignField },
                        { "as", _as }
                    }
                }
            };
        }
    }
}
