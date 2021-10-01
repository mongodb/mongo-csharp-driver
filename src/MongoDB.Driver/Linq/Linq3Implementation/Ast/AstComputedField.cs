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
    internal sealed class AstComputedField : AstNode
    {
        private readonly string _path;
        private readonly AstExpression _value;

        public AstComputedField(string path, AstExpression value)
        {
            _path = Ensure.IsNotNull(path, nameof(path));
            _value = Ensure.IsNotNull(value, nameof(value));
        }

        public override AstNodeType NodeType => AstNodeType.ComputedField;
        public string Path => _path;
        public AstExpression Value => _value;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitComputedField(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument(RenderAsElement());
        }

        public BsonElement RenderAsElement()
        {
            return new BsonElement(_path, _value.Render());
        }

        public override string ToString()
        {
            return $"\"{_path}\" : {_value.Render().ToJson()}";
        }

        public AstComputedField Update(AstExpression value)
        {
            if (value == _value)
            {
                return this;
            }

            return new AstComputedField(_path, value);
        }
    }
}
