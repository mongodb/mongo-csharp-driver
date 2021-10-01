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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters
{
    internal sealed class AstNotFilterOperation : AstFilterOperation
    {
        private readonly AstFilterOperation _operation;

        public AstNotFilterOperation(AstFilterOperation operation)
        {
            _operation = Ensure.IsNotNull(operation, nameof(operation));
        }

        public override AstNodeType NodeType => AstNodeType.NotFilterOperation;
        public AstFilterOperation Operation => _operation;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitNotFilterOperation(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument("$not", _operation.Render());
        }

        public AstNotFilterOperation Update(AstFilterOperation operation)
        {
            if (operation == _operation)
            {
                return this;
            }

            return new AstNotFilterOperation(operation);
        }
    }
}
