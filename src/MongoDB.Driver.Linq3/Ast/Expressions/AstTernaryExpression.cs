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

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    public sealed class AstTernaryExpression : AstExpression
    {
        private readonly AstExpression _arg1;
        private readonly AstExpression _arg2;
        private readonly AstExpression _arg3;
        private readonly AstTernaryOperator _operator;

        public AstTernaryExpression(AstTernaryOperator @operator, AstExpression arg1, AstExpression arg2, AstExpression arg3)
        {
            _operator = @operator;
            _arg1 = Ensure.IsNotNull(arg1, nameof(arg1));
            _arg2 = Ensure.IsNotNull(arg2, nameof(arg2));
            _arg3 = Ensure.IsNotNull(arg3, nameof(arg3));
        }

        public AstExpression Arg1 => _arg1;
        public AstExpression Arg2 => _arg2;
        public AstExpression Arg3 => _arg3;
        public override AstNodeType NodeType => AstNodeType.TernaryExpression;
        public AstTernaryOperator Operator => _operator;

        public override BsonValue Render()
        {
            return new BsonDocument(_operator.Render(), new BsonArray { _arg1.Render(), _arg2.Render(), _arg3.Render() });
        }
    }
}
