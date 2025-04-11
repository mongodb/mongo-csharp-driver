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
    internal sealed class AstConvertExpression : AstExpression
    {
        private readonly ByteOrder? _byteOrder;
        private readonly string _format;
        private readonly AstExpression _input;
        private readonly AstExpression _onError;
        private readonly AstExpression _onNull;
        private readonly BsonBinarySubType? _subType;
        private readonly AstExpression _to;

        public AstConvertExpression(
            AstExpression input,
            AstExpression to,
            BsonBinarySubType? subType = null,
            ByteOrder? byteOrder = null,
            string format = null,
            AstExpression onError = null,
            AstExpression onNull = null)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _to = Ensure.IsNotNull(to, nameof(to));
            _subType = subType;
            _byteOrder = byteOrder;
            _format = format;
            _onError = onError;
            _onNull = onNull;
        }

        public ByteOrder? ByteOrder => _byteOrder;
        public string Format => _format;
        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.ConvertExpression;
        public AstExpression OnError => _onError;
        public AstExpression OnNull => _onNull;
        public BsonBinarySubType? SubType => _subType;
        public AstExpression To => _to;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitConvertExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$convert", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "to", _to.Render(), _subType == null },
                        { "to", () => new BsonDocument
                            {
                                { "type", _to.Render() },
                                { "subtype", (int)_subType!.Value},
                            },
                            _subType != null
                        },
                        { "onError", () => _onError.Render(), _onError != null },
                        { "onNull", () => _onNull.Render(), _onNull != null },
                        { "format", () => _format, _format != null },
                        { "byteOrder", () => _byteOrder!.Value.Render(), _byteOrder != null }
                    }
                }
            };
        }

        public AstConvertExpression Update(
            AstExpression input,
            AstExpression to,
            AstExpression onError,
            AstExpression onNull)
        {
            if (input == _input && to == _to && onError == _onError && onNull == _onNull)
            {
                return this;
            }

            return new AstConvertExpression(input, to, _subType, _byteOrder, _format, onError, onNull);
        }
    }
}
