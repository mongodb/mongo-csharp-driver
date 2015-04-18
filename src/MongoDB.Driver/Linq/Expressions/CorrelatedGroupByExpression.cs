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
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Expressions
{
    internal class CorrelatedGroupByExpression : ExtensionExpression
    {
        private readonly Guid _correlationId;
        private readonly ReadOnlyCollection<Expression> _accumulators;
        private readonly Expression _id;
        private readonly Expression _source;
        private readonly Type _type;

        public CorrelatedGroupByExpression(Guid correlationId, Type type, Expression source, Expression id, IEnumerable<Expression> accumulators)
        {
            _correlationId = correlationId;
            _type = Ensure.IsNotNull(type, "type");
            _source = Ensure.IsNotNull(source, "source");
            _id = Ensure.IsNotNull(id, "idExpression");
            _accumulators = accumulators as ReadOnlyCollection<Expression>;
            if (_accumulators == null)
            {
                _accumulators = new List<Expression>(accumulators).AsReadOnly();
            }
        }

        public ReadOnlyCollection<Expression> Accumulators
        {
            get { return _accumulators; }
        }

        public Guid CorrelationId
        {
            get { return _correlationId; }
        }

        public override ExtensionExpressionType ExtensionType
        {
            get { return ExtensionExpressionType.CorrelatedGroupBy; }
        }

        public Expression Id
        {
            get { return _id; }
        }

        public Expression Source
        {
            get { return _source; }
        }

        public override Type Type
        {
            get { return _type; }
        }

        public CorrelatedGroupByExpression Update(Expression source, Expression id, IEnumerable<Expression> accumulators)
        {
            if (source != _source ||
                id != _id ||
                accumulators != _accumulators)
            {
                return new CorrelatedGroupByExpression(_correlationId, _type, source, id, accumulators);
            }

            return this;
        }

        protected internal override Expression Accept(ExtensionExpressionVisitor visitor)
        {
            return visitor.VisitCorrelatedGroupBy(this);
        }
    }
}
