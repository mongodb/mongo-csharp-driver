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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// A MongoExpression that represents a select.
    /// </summary>
    public class MongoSelectExpression : MongoExpression
    {
        private Expression _from;
        private Expression _where;
        private Expression _skip;
        private Expression _take;

        /// <summary>
        /// Initializes an instance of the MongoSelectExpression class.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="where"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        public MongoSelectExpression(Expression from, Expression where, Expression skip, Expression take)
            : base(MongoExpressionType.Select, typeof(void))
        {
            _from = from;
            _where = where;
            _skip = skip;
            _take = take;
        }

        /// <summary>
        /// Gets the From expression.
        /// </summary>
        public Expression From
        {
            get { return _from; }
        }

        /// <summary>
        /// Gets the Skip expression (returns null if there is none).
        /// </summary>
        public Expression Skip
        {
            get { return _skip; }
        }

        /// <summary>
        /// Gets the Take expression (returns null if there is none).
        /// </summary>
        public Expression Take
        {
            get { return _take; }
        }

        /// <summary>
        /// Gets the Where expresssion (returns null if there is none).
        /// </summary>
        public Expression Where
        {
            get { return _where; }
        }
    }
}
