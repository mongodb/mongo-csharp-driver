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
    internal class SelectManyExpression : ExtensionExpression
    {
        private readonly Expression _collectionSelector;
        private readonly Expression _resultSelector;
        private readonly Expression _source;
        private readonly Type _type;

        public SelectManyExpression(Expression source, Expression collectionSelector, Expression resultSelector)
        {
            _source = Ensure.IsNotNull(source, "source");
            _collectionSelector = Ensure.IsNotNull(collectionSelector, "collectionSelector");
            _resultSelector = Ensure.IsNotNull(resultSelector, "resultSelector");
            _type = typeof(IEnumerable<>).MakeGenericType(resultSelector.Type);
        }

        public override ExtensionExpressionType ExtensionType
        {
            get { return ExtensionExpressionType.SelectMany; }
        }

        public Expression CollectionSelector
        {
            get { return _collectionSelector; }
        }

        public Expression ResultSelector
        {
            get { return _resultSelector; }
        }

        public Expression Source
        {
            get { return _source; }
        }

        public override Type Type
        {
            get { return _type; }
        }

        public override string ToString()
        {
            return string.Format("{0}.SelectMany({1})", _source.ToString(), _resultSelector.ToString());
        }

        public SelectManyExpression Update(Expression source, Expression collectionSelector, Expression resultSelector)
        {
            if (source != _source ||
                collectionSelector != _collectionSelector ||
                resultSelector != _resultSelector)
            {
                return new SelectManyExpression(source, collectionSelector, resultSelector);
            }

            return this;
        }

        protected internal override Expression Accept(ExtensionExpressionVisitor visitor)
        {
            return visitor.VisitSelectMany(this);
        }
    }
}
