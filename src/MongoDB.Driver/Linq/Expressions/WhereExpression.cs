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
using System.Linq.Expressions;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Expressions
{
    internal class WhereExpression : ExtensionExpression
    {
        private readonly Expression _predicate;
        private readonly Expression _source;

        public WhereExpression(Expression source, Expression predicate)
        {
            _predicate = Ensure.IsNotNull(predicate, "predicate");
            _source = Ensure.IsNotNull(source, "source");
        }

        public override ExtensionExpressionType ExtensionType
        {
            get { return ExtensionExpressionType.Where; }
        }

        public Expression Predicate
        {
            get { return _predicate; }
        }

        public Expression Source
        {
            get { return _source; }
        }

        public override Type Type
        {
            get { return _source.Type; }
        }

        public override string ToString()
        {
            return string.Format("{0}.Where({1})", _source.ToString(), _predicate.ToString());
        }

        public WhereExpression Update(Expression source, Expression predicate)
        {
            if (source != _source ||
                predicate != _predicate)
            {
                return new WhereExpression(source, predicate);
            }

            return this;
        }

        protected internal override Expression Accept(ExtensionExpressionVisitor visitor)
        {
            return visitor.VisitWhere(this);
        }
    }
}
