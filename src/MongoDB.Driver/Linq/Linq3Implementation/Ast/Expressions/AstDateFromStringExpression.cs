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
    internal sealed class AstDateFromStringExpression : AstExpression
    {
        private readonly AstExpression _dateString;
        private readonly AstExpression _format;
        private readonly AstExpression _onError;
        private readonly AstExpression _onNull;
        private readonly AstExpression _timezone;

        public AstDateFromStringExpression(
            AstExpression dateString,
            AstExpression format = null,
            AstExpression timezone = null,
            AstExpression onError = null,
            AstExpression onNull = null)
        {
            _dateString = Ensure.IsNotNull(dateString, nameof(dateString));
            _format = format;
            _timezone = timezone;
            _onError = onError;
            _onNull = onNull;
        }

        public AstExpression DateString => _dateString;
        public AstExpression Format => _format;
        public override AstNodeType NodeType => AstNodeType.DateFromStringExpression;
        public AstExpression OnError => _onError;
        public AstExpression OnNull => _onNull;
        public AstExpression Timezone => _timezone;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitDateFromStringExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$dateFromString", new BsonDocument
                    {
                        { "dateString", _dateString.Render() },
                        { "format", () => _format.Render(), _format != null },
                        { "timezone", () => _timezone.Render(), _timezone != null },
                        { "onError", () => _onError.Render(), _onError != null },
                        { "onNull", () => _onNull.Render(), _onNull != null }
                    }
                }
            };
        }

        public AstDateFromStringExpression Update(
            AstExpression dateString,
            AstExpression format,
            AstExpression timezone,
            AstExpression onError,
            AstExpression onNull)
        {
            if (dateString == _dateString && format == _format && timezone == _timezone && onError == _onError && onNull == _onNull)
            {
                return this;
            }

            return new AstDateFromStringExpression(dateString, format, timezone, onError, onNull);
        }
    }
}
