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

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    public sealed class AstConvertExpression : AstExpression
    {
        #region static
        private static AstExpression CreateToExpression(Type toType)
        {
            Ensure.IsNotNull(toType, nameof(toType));
            string to;
            switch (toType.FullName)
            {
                case "MongoDB.Bson.ObjectId": to = "objectId"; break;
                case "System.Boolean": to = "bool"; break;
                case "System.DateTime": to = "date"; break;
                case "System.Decimal": to = "decimal"; break;
                case "System.Double": to = "double"; break;
                case "System.Int32": to = "int"; break;
                case "System.Int64": to = "long"; break;
                case "System.String": to = "string"; break;
                default: throw new ArgumentException($"Invalid toType: {toType.FullName}.", nameof(toType));
            }
            return new AstConstantExpression(to);
        }
        #endregion

        private readonly AstExpression _input;
        private readonly AstExpression _onError;
        private readonly AstExpression _onNull;
        private readonly AstExpression _to;

        public AstConvertExpression(
            AstExpression input,
            AstExpression to,
            AstExpression onError = null,
            AstExpression onNull = null)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _to = Ensure.IsNotNull(to, nameof(to));
            _onError = onError;
            _onNull = onNull;
        }

        public AstConvertExpression(
            AstExpression input,
            Type toType,
            AstExpression onError = null,
            AstExpression onNull = null)
            : this(input, CreateToExpression(toType), onError, onNull)
        {
        }

        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.ConvertExpression;
        public AstExpression OnError => _onError;
        public AstExpression OnNull => _onNull;
        public AstExpression To => _to;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$convert", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "to", _to.Render() },
                        { "onError", () => _onError.Render(), _onError != null },
                        { "onNull", () => _onNull.Render(), _onNull != null }
                    }
                }
            };
        }
    }
}
