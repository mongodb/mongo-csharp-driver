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
    internal sealed class AstDateDiffExpression : AstExpression
    {
        private readonly AstExpression _endDate;
        private readonly AstExpression _startDate;
        private readonly AstExpression _startOfWeek;
        private readonly AstExpression _timezone;
        private readonly AstExpression _unit;

        public AstDateDiffExpression(
            AstExpression startDate,
            AstExpression endDate,
            AstExpression unit,
            AstExpression timezone = null,
            AstExpression startOfWeek = null)
        {
            _startDate = Ensure.IsNotNull(startDate, nameof(startDate));
            _endDate = Ensure.IsNotNull(endDate, nameof(endDate));
            _unit = Ensure.IsNotNull(unit, nameof(unit));
            _timezone = timezone;
            _startOfWeek = startOfWeek;
        }

        public AstExpression EndDate => _endDate;
        public override AstNodeType NodeType => AstNodeType.DateDiffExpression;
        public AstExpression StartDate => _startDate;
        public AstExpression StartOfWeek => _startOfWeek;
        public AstExpression Timezone => _timezone;
        public AstExpression Unit => _unit;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitDateDiffExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$dateDiff", new BsonDocument
                    {
                        { "startDate", _startDate.Render() },
                        { "endDate", _endDate.Render() },
                        { "unit", _unit.Render() },
                        { "timezone", () => _timezone.Render(), _timezone != null },
                        { "startOfWeek", () => _startOfWeek.Render(), _startOfWeek != null }
                    }
                }
            };
        }

        public AstDateDiffExpression Update(
            AstExpression startDate,
            AstExpression endDate,
            AstExpression unit,
            AstExpression timezone,
            AstExpression startOfWeek)
        {
            if (startDate == _startDate && endDate == _endDate && unit == _unit && startOfWeek == _startOfWeek && timezone == _timezone)
            {
                return this;
            }

            return new AstDateDiffExpression(startDate, endDate, unit, timezone, startOfWeek);
        }
    }
}
