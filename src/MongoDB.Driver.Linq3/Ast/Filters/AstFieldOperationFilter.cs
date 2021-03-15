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

namespace MongoDB.Driver.Linq3.Ast.Filters
{
    public sealed class AstFieldOperationFilter : AstFilter
    {
        private readonly AstFilterField _field;
        private readonly AstFilterOperation _operation;

        public AstFieldOperationFilter(AstFilterField field, AstFilterOperation operation)
        {
            _field = Ensure.IsNotNull(field, nameof(field));
            _operation = Ensure.IsNotNull(operation, nameof(operation));
        }

        public AstFilterField Field => _field;
        public override AstNodeType NodeType => AstNodeType.FieldOperationFilter;
        public AstFilterOperation Operation => _operation;

        public override BsonValue Render()
        {
            if (_operation is AstComparisonFilterOperation comparisonOperation &&
                comparisonOperation.Operator == AstComparisonFilterOperator.Eq &&
                comparisonOperation.Value.BsonType != BsonType.RegularExpression)
            {
                return new BsonDocument(_field.Path, comparisonOperation.Value); // implied $eq
            }
            else if(
                _operation is AstElemMatchFilterOperation elemMatchOperation &&
                elemMatchOperation.Filter is AstFieldOperationFilter fieldOperationFilter &&
                fieldOperationFilter.Field.Path == "$elem" &&
                fieldOperationFilter.Operation is AstComparisonFilterOperation elemMatchComparisonOperation &&
                elemMatchComparisonOperation.Operator == AstComparisonFilterOperator.Eq)
            {
                return new BsonDocument(_field.Path, elemMatchComparisonOperation.Value); // implied contains
            }
            else
            {
                return new BsonDocument(_field.Path, _operation.Render());
            }
        }
    }
}
