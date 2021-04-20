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
    internal sealed class AstDateToStringExpression : AstExpression
    {
        private readonly AstExpression _date;
        private readonly AstExpression _format;
        private readonly AstExpression _timezone;
        private readonly AstExpression _onNull;

        public AstDateToStringExpression(
            AstExpression date,
            AstExpression format = null,
            AstExpression timezone = null,
            AstExpression onNull = null)
        {
            _date = Ensure.IsNotNull(date, nameof(date));
            _format = format;
            _timezone = timezone;
            _onNull = onNull;
        }

        public AstExpression Date => _date;
        public AstExpression Format => _format;
        public AstExpression Timezone => _timezone;
        public override AstNodeType NodeType => AstNodeType.DateToStringExpression;
        public AstExpression OnNull => _onNull;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$dateToString", new BsonDocument
                    {
                        { "date", _date.Render() },
                        { "format", () => _format.Render(), _format != null },
                        { "timezone", () => _timezone.Render(), _timezone != null },
                        { "onNull", () => _onNull.Render(), _onNull != null }
                    }
                }
            };
        }
    }
}
