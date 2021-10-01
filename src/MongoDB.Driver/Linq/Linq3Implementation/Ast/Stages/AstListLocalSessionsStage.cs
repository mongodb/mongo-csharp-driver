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
    internal sealed class AstListLocalSessionsStage : AstStage
    {
        private readonly BsonDocument _options;

        public AstListLocalSessionsStage(BsonDocument options)
        {
            _options = Ensure.IsNotNull(options, nameof(options));
        }

        public override AstNodeType NodeType => AstNodeType.ListLocalSessionsStage;
        public BsonDocument Options => _options;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitListLocalSessionsStage(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument("$listLocalSessions", _options);
        }
    }
}
