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
    internal sealed class AstUniversalStage : AstStage
    {
        private readonly BsonDocument _stage;

        public AstUniversalStage(BsonDocument stage)
        {
            _stage = Ensure.IsNotNull(stage, nameof(stage));
        }

        public override AstNodeType NodeType => AstNodeType.UniversalStage;
        public BsonDocument Stage => _stage;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitUniversalStage(this);
        }

        public override BsonValue Render()
        {
            return _stage;
        }

        public AstUniversalStage Update(BsonDocument stage)
        {
            if (stage == _stage)
            {
                return this;
            }

            return new AstUniversalStage(stage);
        }
    }
}
