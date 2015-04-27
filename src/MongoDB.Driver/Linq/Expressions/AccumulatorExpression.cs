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
    internal class AccumulatorExpression : ExtensionExpression
    {
        private readonly AccumulatorType _accumulatorType;
        private readonly Expression _argument;
        private readonly Type _type;

        public AccumulatorExpression(Type type, AccumulatorType accumulatorType, Expression argument)
        {
            _type = Ensure.IsNotNull(type, "type");
            _accumulatorType = accumulatorType;
            _argument = argument;
        }

        public AccumulatorType AccumulatorType
        {
            get { return _accumulatorType; }
        }

        public Expression Argument
        {
            get { return _argument; }
        }

        public override ExtensionExpressionType ExtensionType
        {
            get { return ExtensionExpressionType.Accumulator; }
        }

        public override Type Type
        {
            get { return _type; }
        }

        public override string ToString()
        {
            return string.Format("{0}({1})", _accumulatorType.ToString(), _argument == null ? "" : _argument.ToString());
        }

        public AccumulatorExpression Update(Expression argument)
        {
            if (argument != _argument)
            {
                return new AccumulatorExpression(_type, _accumulatorType, argument);
            }

            return this;
        }

        protected internal override Expression Accept(ExtensionExpressionVisitor visitor)
        {
            return visitor.VisitAccumulator(this);
        }
    }
}
