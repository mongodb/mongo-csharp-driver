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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstConstantExpression : AstExpression
    {
        // public static methods
        // returns true if a constant value has to be quoted (using $literal) when it is rendered in an expression context.
        // the server interprets a string that starts with "$" as a field path or a variable, a document element name that
        // starts with "$" as an operator, a document element name containing "." as a path to a nested field, and the
        // elements of a document or array as nested expressions, so a constant value containing any of those, at any
        // depth, has to be quoted to ensure the server treats it as a value.
        public static bool ValueNeedsToBeQuoted(BsonValue value)
        {
            return value switch
            {
                BsonString stringValue => !IsSafeStringValue(stringValue.Value),
                BsonArray arrayValue => arrayValue.Any(ValueNeedsToBeQuoted),
                BsonDocument documentValue => documentValue.Elements.Any(element => !AstFieldName.IsSafe(element.Name) || ValueNeedsToBeQuoted(element.Value)),
                _ => false
            };

            // a string value is safe unless it starts with "$", which the server reads as a field path or a
            // variable. unlike a field name, a "." in a value has no meaning to the server, and an empty
            // string is a perfectly good value.
            static bool IsSafeStringValue(string value) => !value.StartsWith("$", StringComparison.Ordinal);
        }

        private readonly BsonValue _value;

        public AstConstantExpression(BsonValue value)
        {
            _value = value;
        }

        public override AstNodeType NodeType => AstNodeType.ConstantExpression;
        public BsonValue Value => _value;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitConstantExpression(this);
        }

        public override BsonValue Render()
        {
            if (ValueNeedsToBeQuoted(_value))
            {
                return new BsonDocument("$literal", _value);
            }

            return _value;
        }
    }
}
