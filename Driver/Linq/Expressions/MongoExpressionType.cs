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
    /// Represents additional Expression node types for MongoExpression nodes in an expression tree.
    /// </summary>
    public enum MongoExpressionType
    {
        // note: make sure these values don't conflict with ExpressionType

        /// <summary>
        /// An operation that references a MongoDB collection.
        /// </summary>
        Collection = 1000,

        /// <summary>
        /// An operation that references a MongoDB field.
        /// </summary>
        Field,

        /// <summary>
        /// A select operation against MongoDB.
        /// </summary>
        Select,

        /// <summary>
        /// A projection operation against MongoDB.
        /// </summary>
        Projection
    }
}
