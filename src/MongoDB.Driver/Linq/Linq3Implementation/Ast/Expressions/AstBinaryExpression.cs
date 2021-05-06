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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstBinaryExpression : AstExpression
    {
        private readonly AstExpression _arg1;
        private readonly AstExpression _arg2;
        private readonly AstBinaryOperator _operator;

        public AstBinaryExpression(AstBinaryOperator @operator, AstExpression arg1, AstExpression arg2)
        {
            _operator = @operator;
            _arg1 = Ensure.IsNotNull(arg1, nameof(arg1));
            _arg2 = Ensure.IsNotNull(arg2, nameof(arg2));
        }

        public AstExpression Arg1 => _arg1;
        public AstExpression Arg2 => _arg2;
        public override AstNodeType NodeType => AstNodeType.BinaryExpression;
        public AstBinaryOperator Operator => _operator;

        public override BsonValue Render()
        {
            return new BsonDocument(_operator.Render(), new BsonArray { _arg1.Render(), _arg2.Render() });
        }
    }
}
