﻿/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters
{
    internal sealed class AstFieldOperationFilter : AstFilter
    {
        private readonly AstFilterField _field;
        private readonly AstFilterOperation _operation;

        public AstFieldOperationFilter(AstFilterField field, AstFilterOperation operation)
        {
            _field = Ensure.IsNotNull(field, nameof(field));
            _operation = Ensure.IsNotNull(operation, nameof(operation));

            if (operation.NodeType == AstNodeType.RegexFilterOperation &&
                field.Serializer is IRepresentationConfigurable representationConfigurable &&
                representationConfigurable.Representation != BsonType.String)
            {
                // normally an ExpressionNotSupported should have been thrown before reaching here
                throw new ArgumentException($"Field must be represented as a string for regex filter operations: {field.Path}", nameof(field));
            }

            if (_field.Path == "@<current>")
            {
                throw new ExpressionNotSupportedException("Field path cannot be \"@<current>\" in AstFieldOperationFilter.");
            }
        }

        public new AstFilterField Field => _field;
        public override AstNodeType NodeType => AstNodeType.FieldOperationFilter;
        public AstFilterOperation Operation => _operation;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitFieldOperationFilter(this);
        }

        public override BsonValue Render()
        {
            var fieldPath = _field.Path;
            if (fieldPath == "@<elem>")
            {
                return _operation.Render();
            }
            if (fieldPath.StartsWith("@<elem>."))
            {
                fieldPath = fieldPath.Substring(8);
            }

            if (_operation is AstComparisonFilterOperation comparisonOperation &&
                comparisonOperation.Operator == AstComparisonFilterOperator.Eq &&
                comparisonOperation.Value.BsonType != BsonType.RegularExpression)
            {
                return new BsonDocument(fieldPath, comparisonOperation.Value); // implied $eq
            }

            if (
                _operation is AstElemMatchFilterOperation elemMatchOperation &&
                elemMatchOperation.Filter is AstFieldOperationFilter fieldOperationFilter &&
                fieldOperationFilter.Field.Path == "@<elem>")
            {
                if (fieldOperationFilter.Operation is AstComparisonFilterOperation elemMatchComparisonOperation &&
                    elemMatchComparisonOperation.Operator == AstComparisonFilterOperator.Eq &&
                    elemMatchComparisonOperation.Value.BsonType != BsonType.RegularExpression)
                {
                    return new BsonDocument(fieldPath, elemMatchComparisonOperation.Value); // implied $elemMatch with $eq
                }

                if (fieldOperationFilter.Operation is AstRegexFilterOperation elemMatchRegexOperation)
                {
                    return new BsonDocument(fieldPath, elemMatchRegexOperation.Render()); // implied $elemMatch with $regex
                }
            }

            return new BsonDocument(fieldPath, _operation.Render());
        }

        public AstFieldOperationFilter Update(AstFilterField field, AstFilterOperation operation)
        {
            if (field == _field && operation == _operation)
            {
                return this;
            }

            return new AstFieldOperationFilter(field, operation);
        }
    }
}
