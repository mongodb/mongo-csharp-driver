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

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// A MongoExpression that represents a projection.
    /// </summary>
    public class MongoProjectionExpression : MongoExpression
    {
        private Expression _projector;
        private MongoSelectExpression _source;

        /// <summary>
        /// Initializes an instance of the MongoProjectionExpression class.
        /// </summary>
        /// <param name="source">The select expression that is the source of the data</param>
        /// <param name="projector">The expression that does the projection.</param>
        public MongoProjectionExpression(MongoSelectExpression source, Expression projector)
            : base(MongoExpressionType.Projection, typeof(IEnumerable<>).MakeGenericType(projector.Type))
        {
            _source = source;
            _projector = projector;
        }

        /// <summary>
        /// Gets the expression that does the projection.
        /// </summary>
        public Expression Projector
        {
            get { return _projector; }
        }

        /// <summary>
        /// Gets the select expression that is the source of data for the projection.
        /// </summary>
        public MongoSelectExpression Source
        {
            get { return _source; }
        }
    }
}
