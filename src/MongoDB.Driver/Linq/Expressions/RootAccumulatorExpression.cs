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
    internal class RootAccumulatorExpression : ExtensionExpression
    {
        private readonly Expression _accumulator;
        private readonly Expression _source;

        public RootAccumulatorExpression(Expression source, Expression accumulator)
        {
            _source = Ensure.IsNotNull(source, "source");
            _accumulator = Ensure.IsNotNull(accumulator, "accumulator");
        }

        public Expression Accumulator
        {
            get { return _accumulator; }
        }

        public override ExtensionExpressionType ExtensionType
        {
            get { return ExtensionExpressionType.RootAccumulator; }
        }

        public Expression Source
        {
            get { return _source; }
        }

        public override Type Type
        {
            get { return _accumulator.Type; }
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}",
                _source.ToString(),
                _accumulator.ToString());
        }

        public RootAccumulatorExpression Update(Expression source, Expression accumulator)
        {
            if (source != _source || accumulator != _accumulator)
            {
                return new RootAccumulatorExpression(source, _accumulator);
            }

            return this;
        }

        protected internal override Expression Accept(ExtensionExpressionVisitor visitor)
        {
            return visitor.VisitRootAccumulator(this);
        }
    }
}
