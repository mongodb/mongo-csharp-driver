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

using System;

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    internal enum AstDatePart
    {
        DayOfMonth,
        DayOfWeek,
        DayOfYear,
        Hour,
        IsoDayOfWeek,
        IsoWeek,
        IsoWeekYear,
        Millisecond,
        Minute,
        Month,
        Second,
        Week,
        Year
    }

    internal static class AstDatePartExtensions
    {
        public static string Render(this AstDatePart part)
        {
            switch (part)
            {
                case AstDatePart.DayOfMonth: return "$dayOfMonth";
                case AstDatePart.DayOfWeek: return "$dayOfWeek";
                case AstDatePart.DayOfYear: return "$dayOfYear";
                case AstDatePart.Hour: return "$hour";
                case AstDatePart.IsoDayOfWeek: return "$isoDayOfWeek";
                case AstDatePart.IsoWeek: return "$isoWeek";
                case AstDatePart.IsoWeekYear: return "$isoWeekYear";
                case AstDatePart.Millisecond: return "$millisecond";
                case AstDatePart.Minute: return "$minute";
                case AstDatePart.Month: return "$month";
                case AstDatePart.Second: return "$second";
                case AstDatePart.Week: return "$week";
                case AstDatePart.Year: return "$year";
                default: throw new InvalidOperationException($"Unexpected date part: {part}.");
            }
        }
    }
}
