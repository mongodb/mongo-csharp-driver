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

using System.Linq.Expressions;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Expressions
{
    internal class TakeExpression : ExtensionExpression
    {
        private readonly Expression _source;
        private readonly int _count;

        public TakeExpression(Expression source, int count)
        {
            _source = Ensure.IsNotNull(source, "source");
            _count = count;
        }

        public int Count
        {
            get { return _count; }
        }

        public Expression Source
        {
            get { return _source; }
        }

        public override ExtensionExpressionType ExtensionType
        {
            get { return ExtensionExpressionType.Take; }
        }

        public override string ToString()
        {
            return string.Format("{0}.Take({1})", _source.ToString(), _count.ToString());
        }

        public TakeExpression Update(Expression source, int limit)
        {
            if (source != _source ||
                limit != _count)
            {
                return new TakeExpression(source, limit);
            }

            return this;
        }

        protected internal override Expression Accept(ExtensionExpressionVisitor visitor)
        {
            return visitor.VisitTake(this);
        }
    }
}
