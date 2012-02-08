﻿/* Copyright 2010-2012 10gen Inc.
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
using System.Text;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Wrappers
{
    /// <summary>
    /// Represents a wrapped object that can be used where an IMongoGroupBy is expected (the wrapped object is expected to serialize properly).
    /// </summary>
    public class GroupByWrapper : BaseWrapper, IMongoGroupBy
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the GroupByWrapper class.
        /// </summary>
        /// <param name="groupBy">The wrapped object.</param>
        public GroupByWrapper(object groupBy)
            : base(groupBy)
        {
        }

        // public static methods
        /// <summary>
        /// Creates a new instance of the GroupByWrapper class.
        /// </summary>
        /// <param name="groupBy">The wrapped object.</param>
        /// <returns>A new instance of GroupByWrapper or null.</returns>
        public static GroupByWrapper Create(object groupBy)
        {
            if (groupBy == null)
            {
                return null;
            }
            else
            {
                return new GroupByWrapper(groupBy);
            }
        }
    }
}
