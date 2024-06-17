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
    internal sealed class AstSortArrayExpression : AstExpression
    {
        private readonly AstSortFields _fields;
        private readonly AstExpression _input;
        private readonly AstSortOrder _order;

        public AstSortArrayExpression(AstExpression input, AstSortFields fields)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _fields = Ensure.IsNotNull(fields, nameof(fields));
        }

        public AstSortArrayExpression(AstExpression input, AstSortOrder order)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _order = Ensure.IsNotNull(order, nameof(order));
        }

        public AstSortFields Fields => _fields;
        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.SortArrayExpression;
        public AstSortOrder Order => _order;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitSortArrayExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument(
                "$sortArray",
                new BsonDocument
                {
                    { "input", _input.Render() },
                    { "sortBy", _fields?.Render(), _fields != null },
                    { "sortBy", _order?.Render(), _order != null }
                });
        }

        public AstSortArrayExpression Update(AstExpression input, AstSortFields fields, AstSortOrder order)
        {
            if (input == _input && fields == _fields && order == _order)
            {
                return this;
            }

            return (fields, order) switch
            {
                (null, null) => throw new ArgumentException("fields and order arguments cannot both be null."),
                (_, null) => new AstSortArrayExpression(input, fields),
                (null, _) => new AstSortArrayExpression(input, order),
                (_, _) => throw new ArgumentException("fields and order arguments are mutually exclusive.")
            };
        }
    }
}
