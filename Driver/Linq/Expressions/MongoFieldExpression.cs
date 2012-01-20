/* Copyright 2010-2012 10gen Inc.
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
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// A MongoExpression that represents a field reference.
    /// </summary>
    public class MongoFieldExpression : MongoExpression
    {
        private Expression _expression;
        private string _name;
        private BsonMemberMap _memberMap;

        /// <summary>
        /// Initializes an instance of the MongoFieldExpression class.
        /// </summary>
        /// <param name="expression">The expression that references the field.</param>
        /// <param name="name">The name of the referenced field.</param>
        /// <param name="memberMap">The BsonMemberMap of the referenced field.</param>
        public MongoFieldExpression(Expression expression, string name, BsonMemberMap memberMap)
            : base(MongoExpressionType.Field, expression.Type)
        {
            _expression = expression;
            _name = name;
            _memberMap = memberMap;
        }

        /// <summary>
        /// Gets the expression that references the field.
        /// </summary>
        public Expression Expression
        {
            get { return _expression; }
        }

        /// <summary>
        /// Gets the BsonMemberMap of the referenced field.
        /// </summary>
        public BsonMemberMap MemberMap
        {
            get { return _memberMap; }
        }

        /// <summary>
        /// Gets the name of the referenced field.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }
    }
}
