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
    public sealed class AstDatePartExpression : AstExpression
    {
        private readonly AstExpression _date;
        private readonly AstDatePart _part;
        private readonly AstExpression _timezone;

        public AstDatePartExpression(
            AstDatePart part,
            AstExpression date,
            AstExpression timezone = null)
        {
            _part = part;
            _date = Ensure.IsNotNull(date, nameof(date));
            _timezone = timezone;
        }

        public AstExpression Date => _date;
        public override AstNodeType NodeType => AstNodeType.DatePartExpression;
        public AstDatePart Part => _part;
        public AstExpression Timezone => _timezone;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { _part.Render(), RenderArgs() }
            };
        }

        private BsonValue RenderArgs()
        {
            if (_timezone == null)
            {
                return _date.Render();
            }
            else
            {
                return new BsonDocument
                {
                    { "date", _date.Render() },
                    { "timezone", _timezone.Render() }
                };
            }
        }
    }
}
