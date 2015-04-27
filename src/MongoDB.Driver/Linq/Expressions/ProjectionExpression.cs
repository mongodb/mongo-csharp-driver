/* Copyright 2010-2014 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Expressions
{
    internal class ProjectionExpression : ExtensionExpression
    {
        private readonly Expression _source;
        private readonly Expression _projector;
        private readonly LambdaExpression _aggregator;
        private readonly Type _type;

        public ProjectionExpression(Expression source, Expression projector)
            : this(source, projector, null)
        {
        }

        public ProjectionExpression(Expression source, Expression projector, LambdaExpression aggregator)
        {
            _source = Ensure.IsNotNull(source, "source");
            _projector = Ensure.IsNotNull(projector, "projector");
            _aggregator = aggregator;

            _type = _aggregator != null ?
                _aggregator.Body.Type :
                typeof(IEnumerable<>).MakeGenericType(_projector.Type);
        }

        public LambdaExpression Aggregator
        {
            get { return _aggregator; }
        }

        public Expression Projector
        {
            get { return _projector; }
        }

        public Expression Source
        {
            get { return _source; }
        }

        public override ExtensionExpressionType ExtensionType
        {
            get { return ExtensionExpressionType.Projection; }
        }

        public override Type Type
        {
            get { return _type; }
        }

        public override string ToString()
        {
            var str = _source.ToString();
            if (_aggregator != null)
            {
                return str + "(" + _aggregator.ToString() + ")";
            }

            return str;
        }

        public ProjectionExpression Update(Expression source, Expression projector, LambdaExpression aggregator)
        {
            if (source != _source ||
                projector != _projector ||
                aggregator != _aggregator)
            {
                return new ProjectionExpression(source, projector, aggregator);
            }

            return this;
        }

        protected internal override Expression Accept(ExtensionExpressionVisitor visitor)
        {
            return visitor.VisitProjection(this);
        }
    }
}
