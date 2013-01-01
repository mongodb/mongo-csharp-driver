/* Copyright 2010-2013 10gen Inc.
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

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Represents an order by clause.
    /// </summary>
    public class OrderByClause
    {
        // private fields
        private LambdaExpression _key;
        private OrderByDirection _direction;

        // constructors
        /// <summary>
        /// Initializes an instance of the OrderByClause class.
        /// </summary>
        /// <param name="key">An expression identifying the key of the order by clause.</param>
        /// <param name="direction">The direction of the order by clause.</param>
        public OrderByClause(LambdaExpression key, OrderByDirection direction)
        {
            _key = key;
            _direction = direction;
        }

        // public properties
        /// <summary>
        /// Gets the lambda expression identifying the key of the order by clause.
        /// </summary>
        public LambdaExpression Key
        {
            get { return _key; }
        }

        /// <summary>
        /// Gets the direction of the order by clause.
        /// </summary>
        public OrderByDirection Direction
        {
            get { return _direction; }
        }
    }
}
