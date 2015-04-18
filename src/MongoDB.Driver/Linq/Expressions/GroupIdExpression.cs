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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Expressions
{
    internal class GroupIdExpression : ExtensionExpression, ISerializationExpression
    {
        private readonly Expression _expression;
        private readonly BsonSerializationInfo _serializationInfo;

        public GroupIdExpression(Expression expression, BsonSerializationInfo serializationInfo)
        {
            _expression = Ensure.IsNotNull(expression, "expression");
            _serializationInfo = Ensure.IsNotNull(serializationInfo, "serializationInfo");
        }

        public Expression Expression
        {
            get { return _expression; }
        }

        public override ExtensionExpressionType ExtensionType
        {
            get { return ExtensionExpressionType.GroupId; }
        }

        public BsonSerializationInfo SerializationInfo
        {
            get { return _serializationInfo; }
        }

        public override Type Type
        {
            get { return _expression.Type; }
        }

        public override string ToString()
        {
            return "[" + _expression.ToString() + "]";
        }

        public GroupIdExpression Update(Expression expression)
        {
            if (expression != _expression)
            {
                return new GroupIdExpression(expression, _serializationInfo);
            }

            return this;
        }

        protected internal override Expression Accept(ExtensionExpressionVisitor visitor)
        {
            return visitor.VisitGroupId(this);
        }
    }
}
