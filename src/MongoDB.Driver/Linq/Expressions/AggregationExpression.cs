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

namespace MongoDB.Driver.Linq.Expressions
{
    internal class AggregationExpression : MongoExpression
    {
        private readonly AggregationType _aggregationType;
        private readonly Expression _argument;
        private readonly Type _resultType;

        public AggregationExpression(Type resultType, AggregationType aggregationType, Expression argument)
        {
            _resultType = resultType;
            _aggregationType = aggregationType;
            _argument = argument;
        }

        public AggregationType AggregationType
        {
            get { return _aggregationType; }
        }

        public Expression Argument
        {
            get { return _argument; }
        }

        public override MongoExpressionType MongoNodeType
        {
            get { return MongoExpressionType.Aggregation; }
        }

        public override Type Type
        {
            get { return _resultType; }
        }
    }
}
