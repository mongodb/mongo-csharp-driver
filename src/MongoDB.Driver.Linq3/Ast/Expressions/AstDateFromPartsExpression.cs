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
using System;

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    public sealed class AstDateFromPartsExpression : AstExpression
    {
        private readonly AstExpression _day;
        private readonly AstExpression _hour;
        private readonly AstExpression _millisecond;
        private readonly AstExpression _minute;
        private readonly AstExpression _month;
        private readonly AstExpression _second;
        private readonly AstExpression _timezone;
        private readonly AstExpression _year;

        public AstDateFromPartsExpression(
            AstExpression year,
            AstExpression month = null,
            AstExpression day = null,
            AstExpression hour = null,
            AstExpression minute = null,
            AstExpression second = null,
            AstExpression millisecond = null,
            AstExpression timezone = null)
        {
            _year = Ensure.IsNotNull(year, nameof(year));
            _month = month;
            _day = day;
            _hour = hour;
            _minute = minute;
            _second = second;
            _millisecond = millisecond;
            _timezone = timezone;
        }

        public AstExpression Day => _day;
        public AstExpression Hour => _hour;
        public AstExpression Millisecond => _millisecond;
        public AstExpression Minute => _minute;
        public AstExpression Month => _month;
        public override AstNodeType NodeType => AstNodeType.DateFromPartsExpression;
        public AstExpression Second => _second;
        public AstExpression Year => _year;
        public AstExpression Timezone => _timezone;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$dateFromParts", new BsonDocument
                    {
                        { "year", _year.Render() },
                        { "month", () => _month.Render(), _month != null },
                        { "day", () => _day.Render(), _day != null },
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
