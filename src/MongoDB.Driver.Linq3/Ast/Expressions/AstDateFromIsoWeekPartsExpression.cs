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

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    public sealed class AstDateFromIsoWeekPartsExpression : AstExpression
    {
        private readonly AstExpression _hour;
        private readonly AstExpression _isoDayOfWeek;
        private readonly AstExpression _isoWeek;
        private readonly AstExpression _isoWeekYear;
        private readonly AstExpression _millisecond;
        private readonly AstExpression _minute;
        private readonly AstExpression _second;
        private readonly AstExpression _timezone;

        public AstDateFromIsoWeekPartsExpression(
            AstExpression isoWeekYear,
            AstExpression isoWeek = null,
            AstExpression isoDayOfWSeek = null,
            AstExpression hour = null,
            AstExpression minute = null,
            AstExpression second = null,
            AstExpression millisecond = null,
            AstExpression timezone = null)
        {
            _isoWeekYear = Ensure.IsNotNull(isoWeekYear, nameof(isoWeekYear));
            _isoWeek = isoWeek;
            _isoDayOfWeek = isoDayOfWSeek;
            _hour = hour;
            _minute = minute;
            _second = second;
            _millisecond = millisecond;
            _timezone = timezone;
        }

        public AstExpression Hour => _hour;
        public AstExpression IsoDayOfWeek => _isoDayOfWeek;
        public AstExpression IsoWeek => _isoWeek;
        public AstExpression IsoWeekYear => _isoWeekYear;
        public AstExpression Millisecond => _millisecond;
        public AstExpression Minute => _minute;
        public override AstNodeType NodeType => AstNodeType.DateFromIsoWeekPartsExpression;
        public AstExpression Second => _second;
        public AstExpression Timezone => _timezone;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$dateFromParts", new BsonDocument
                    {
                        { "isoWeekYear", _isoWeekYear.Render() },
                        { "isoWeek", () => _isoWeek.Render(), _isoWeek != null },
                        { "isoDayOfWeek", () => _isoDayOfWeek.Render(), _isoDayOfWeek != null },
                        { "hour", () => _hour.Render(), _hour != null },
                        { "minute", () => _minute.Render(), _minute != null },
                        { "second", () => _second.Render(), _second != null },
                        { "millisecond", () => _millisecond.Render(), _millisecond != null },
                        { "timezone", () => _timezone.Render(), _timezone != null }
                    }
                }
            };
        }
    }
}
