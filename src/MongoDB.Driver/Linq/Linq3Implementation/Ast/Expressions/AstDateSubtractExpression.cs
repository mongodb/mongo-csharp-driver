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
    internal sealed class AstDateSubtractExpression : AstExpression
    {
        private readonly AstExpression _amount;
        private readonly AstExpression _startDate;
        private readonly AstExpression _timezone;
        private readonly AstExpression _unit;

        public AstDateSubtractExpression(
            AstExpression startDate,
            AstExpression unit,
            AstExpression amount,
            AstExpression timezone = null)
        {
            _startDate = Ensure.IsNotNull(startDate, nameof(startDate));
            _unit = Ensure.IsNotNull(unit, nameof(unit));
            _amount = Ensure.IsNotNull(amount, nameof(amount));
            _timezone = timezone;
        }

        public AstExpression Amount => _amount;
        public override AstNodeType NodeType => AstNodeType.DateSubtractExpression;
        public AstExpression StartDate => _startDate;
        public AstExpression Timezone => _timezone;
        public AstExpression Unit => _unit;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitDateSubtractExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$dateSubtract", new BsonDocument
                    {
                        { "startDate", _startDate.Render() },
                        { "unit", _unit.Render() },
                        { "amount", _amount.Render() },
                        { "timezone", () => _timezone.Render(), _timezone != null }
                    }
                }
            };
        }

        public AstDateSubtractExpression Update(
            AstExpression startDate,
            AstExpression unit,
            AstExpression amount,
            AstExpression timezone)
        {
            if (startDate == _startDate && unit == _unit && amount == _amount && timezone == _timezone)
            {
                return this;
            }

            return new AstDateSubtractExpression(startDate, unit, amount, timezone);
        }
    }
}
