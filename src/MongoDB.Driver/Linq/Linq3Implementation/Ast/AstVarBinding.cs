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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast
{
    internal sealed class AstVarBinding : AstNode
    {
        private readonly AstExpression _value;
        private readonly AstVarExpression _var;

        public AstVarBinding(AstVarExpression var, AstExpression value)
        {
            _var = Ensure.IsNotNull(var, nameof(var));
            _value = Ensure.IsNotNull(value, nameof(value));
        }

        public override AstNodeType NodeType => AstNodeType.VarBinding;
        public AstExpression Value => _value;
        public AstVarExpression Var => _var;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitVarBinding(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument(RenderAsElement());
        }

        public BsonElement RenderAsElement()
        {
            return new BsonElement(_var.Name, _value.Render());
        }

        public override string ToString()
        {
            return $"\"{_var.Name}\" : {_value.Render().ToJson()}";
        }

        public AstVarBinding Update(AstVarExpression var, AstExpression value)
        {
            if (var == _var && value == _value)
            {
                return this;
            }

            return new AstVarBinding(_var, value);
        }
    }
}
