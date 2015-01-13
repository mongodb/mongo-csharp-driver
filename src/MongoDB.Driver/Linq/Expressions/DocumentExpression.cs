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

namespace MongoDB.Driver.Linq.Expressions
{
    internal class DocumentExpression : MongoExpression, IBsonSerializationInfoExpression
    {
        private readonly Expression _expression;
        private readonly bool _isProjected;
        private readonly BsonSerializationInfo _serializationInfo;

        public DocumentExpression(Expression expression, BsonSerializationInfo serializationInfo, bool isProjected)
        {
            _expression = expression;
            _serializationInfo = serializationInfo;
            _isProjected = isProjected;
        }

        public Expression Expression
        {
            get { return _expression; }
        }

        public bool IsProjected
        {
            get { return _isProjected; }
        }

        public override MongoExpressionType MongoNodeType
        {
            get { return MongoExpressionType.Document; }
        }

        public BsonSerializationInfo SerializationInfo
        {
            get { return _serializationInfo; }
        }

        public override Type Type
        {
            get { return _expression.Type; }
        }
    }
}
