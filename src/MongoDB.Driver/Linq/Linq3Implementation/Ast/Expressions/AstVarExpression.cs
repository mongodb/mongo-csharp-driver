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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstVarExpression : AstExpression
    {
        private readonly bool _isCurrent;
        private readonly string _name;

        public AstVarExpression(string name, bool isCurrent = false)
        {
            _name = Ensure.IsNotNullOrEmpty(name, nameof(name));
            _isCurrent = isCurrent;
        }

        public bool IsCurrent => _isCurrent;
        public string Name => _name;
        public override AstNodeType NodeType => AstNodeType.VarExpression;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitVarExpression(this);
        }

        public AstVarExpression AsNotCurrent()
        {
            return _isCurrent ? new AstVarExpression(_name, isCurrent: false) : this;
        }

        public override bool CanBeConvertedToFieldPath()
        {
            return true;
        }

        public override string ConvertToFieldPath()
        {
            return "$$" + _name;
        }

        public override BsonValue Render()
        {
            return "$$" + _name;
        }
    }
}
