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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstConvertExpression : AstExpression
    {
        private readonly AstExpression _input;
        private readonly string _format;
        private readonly AstExpression _onError;
        private readonly AstExpression _onNull;
        private readonly AstExpression _to;
        private readonly AstExpression _subType;
        private readonly Mql.ByteOrder? _byteOrder;

        public AstConvertExpression(
            AstExpression input,
            AstExpression to,
            AstExpression onError = null,
            AstExpression onNull = null,
            AstExpression subType = null,
            string format = null,
            Mql.ByteOrder? byteOrder = null)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _to = Ensure.IsNotNull(to, nameof(to));
            _onError = onError;
            _onNull = onNull;
            _subType = subType;
            _format = format;
            _byteOrder = byteOrder;
        }

        public Mql.ByteOrder? ByteOrder => _byteOrder;
        public AstExpression Input => _input;
        public string Format => _format;
        public override AstNodeType NodeType => AstNodeType.ConvertExpression;
        public AstExpression OnError => _onError;
        public AstExpression OnNull => _onNull;
        public AstExpression To => _to;
        public AstExpression SubType => _subType;

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
                                {"type", _to.Render() },
                                {"subtype", _subType.Render()},
                            }, _subType != null
                        },
                        { "onError", () => _onError.Render(), _onError != null },
                        { "onNull", () => _onNull.Render(), _onNull != null },
                        { "format", () => _format, _format != null},
                        { "byteOrder", () => MapMqlByteOrderToString(_byteOrder!.Value), _byteOrder != null}
                    }
                }
            };
        }

        public AstConvertExpression Update(
            AstExpression input,
            AstExpression to,
            AstExpression onError,
            AstExpression onNull,
            AstExpression subType,
            string format,
            Mql.ByteOrder? byteOrder)
        {
            if (input == _input && to == _to && onError == _onError && onNull == _onNull &&
                subType == _subType && format == _format && byteOrder == _byteOrder)
            {
                return this;
            }

            return new AstConvertExpression(input, to, onError, onNull, subType, format, byteOrder);
        }

        private static string MapMqlByteOrderToString(Mql.ByteOrder byteOrder)
        {
            return byteOrder switch
            {
                Mql.ByteOrder.BigEndian => "big",
                Mql.ByteOrder.LittleEndian => "little",
                _ => throw new ArgumentException($"Unexpected Mql.ByteOrder: {byteOrder}.", nameof(byteOrder))
            };
        }
    }
}
