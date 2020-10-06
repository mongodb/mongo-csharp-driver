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
using MongoDB.Driver.Linq3.Ast.Expressions;

namespace MongoDB.Driver.Linq3.Ast.Filters
{
    public sealed class AstComparisonFilter : AstFilter
    {
        private readonly AstFieldExpression _field;
        private readonly AstComparisonFilterOperator _operator;
        private readonly BsonValue _value;

        public AstComparisonFilter(AstComparisonFilterOperator @operator, AstFieldExpression field, BsonValue value)
        {
            _operator = @operator;
            _field = Ensure.IsNotNull(field, nameof(field));
            _value = Ensure.IsNotNull(value, nameof(value));
        }

        public AstFieldExpression Field => _field;
        public override AstNodeType NodeType => AstNodeType.ComparisonFilter;
        public AstComparisonFilterOperator Operator => _operator;
        public BsonValue Value => _value;

        public override BsonValue Render()
        {
            if (_operator == AstComparisonFilterOperator.Eq)
            {
                return new BsonDocument(_field.Render().AsString, _value);
            }
            else
            {
                return new BsonDocument(_field.Render().AsString, new BsonDocument(_operator.Render(), _value));
            }
        }
    }
}
