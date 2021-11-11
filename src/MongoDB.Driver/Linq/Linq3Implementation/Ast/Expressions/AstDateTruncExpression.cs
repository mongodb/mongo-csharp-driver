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
    internal sealed class AstDateTruncExpression : AstExpression
    {
        private readonly AstExpression _binSize;
        private readonly AstExpression _date;
        private readonly AstExpression _startOfWeek;
        private readonly AstExpression _timezone;
        private readonly AstExpression _unit;

        public AstDateTruncExpression(
            AstExpression date,
            AstExpression unit,
            AstExpression binSize = null,
            AstExpression timezone = null,
            AstExpression startOfWeek = null)
        {
            _date = Ensure.IsNotNull(date, nameof(date));
            _unit = Ensure.IsNotNull(unit, nameof(unit));
            _binSize = binSize;
            _timezone = timezone;
            _startOfWeek = startOfWeek;
        }

        public AstExpression BinSize => _binSize;
        public AstExpression Date => _date;
        public override AstNodeType NodeType => AstNodeType.DateTruncExpression;
        public AstExpression StartOfWeek => _startOfWeek;
        public AstExpression Timezone => _timezone;
        public AstExpression Unit => _unit;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitDateTruncExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$dateTrunc", new BsonDocument
                    {
                        { "date", _date.Render() },
                        { "unit", _unit.Render() },
                        { "binSize", () => _binSize.Render(), _binSize != null },
                        { "timezone", () => _timezone.Render(), _timezone != null },
                        { "startOfWeek", () => _startOfWeek.Render(), _startOfWeek != null }
                    }
                }
            };
        }

        public AstDateTruncExpression Update(
            AstExpression date,
            AstExpression unit,
            AstExpression binSize,
            AstExpression timezone,
            AstExpression startOfWeek)
        {
            if (date == _date && unit == _unit && binSize == _binSize && timezone == _timezone && startOfWeek == _startOfWeek)
            {
                return this;
            }

            return new AstDateTruncExpression(date, unit, binSize, timezone, startOfWeek);
        }
    }
}
