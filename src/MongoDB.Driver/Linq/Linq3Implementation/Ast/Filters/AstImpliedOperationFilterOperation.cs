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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters
{
    internal sealed class AstImpliedOperationFilterOperation : AstFilterOperation
    {
        // public static methods
        // an implied operation renders its value verbatim in the position where the server decides how to read it.
        // a document whose first element name starts with "$" is read there as a set of query operators rather than
        // as a value to compare against, and unlike an aggregation expression the query language has no $literal
        // escape, so such a value can never be represented as an implied operation. a regular expression can be:
        // it renders as { field : /pattern/options }, which is a regex match rather than an injected operator.
        public static bool CanRepresent(BsonValue value)
        {
            return
                value is not BsonDocument document ||
                document.ElementCount == 0 ||
                !document.GetElement(0).Name.StartsWith("$", StringComparison.Ordinal);
        }

        private readonly BsonValue _value;

        public AstImpliedOperationFilterOperation(BsonValue value)
        {
            _value = Ensure.IsNotNull(value, nameof(value));

            // last line of defense: a rewrite that promotes a value to an implied operation must ask CanRepresent
            // first and decline to rewrite when it returns false, so reaching this is a bug in the caller
            if (!CanRepresent(_value))
            {
                var firstElementName = ((BsonDocument)_value).GetElement(0).Name;
                throw new ArgumentException(
                    $"An implied operation cannot represent a document whose first element name is \"{firstElementName}\" because the server would interpret the document as query operators.",
                    nameof(value));
            }
        }

        public override AstNodeType NodeType => AstNodeType.ImpliedOperationFilterOperation;
        public BsonValue Value => _value;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitImpliedOperationFilterOperation(this);
        }

        public override BsonValue Render()
        {
            return _value;
        }

        public AstImpliedOperationFilterOperation Update(BsonValue value)
        {
            if (value == _value)
            {
                return this;
            }

            return new AstImpliedOperationFilterOperation(value);
        }
    }
}
