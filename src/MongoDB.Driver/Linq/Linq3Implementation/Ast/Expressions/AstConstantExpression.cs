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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstConstantExpression : AstExpression
    {
        private readonly BsonValue _value;

        public AstConstantExpression(BsonValue value)
        {
            _value = value;
        }

        public override AstNodeType NodeType => AstNodeType.ConstantExpression;
        public BsonValue Value => _value;

        public override BsonValue Render()
        {
            if (_value is BsonString bsonString && bsonString.Value.StartsWith("$"))
            {
                return new BsonDocument("$literal", _value);
            }

            return _value;
        }
    }
}
