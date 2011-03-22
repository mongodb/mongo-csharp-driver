﻿/* Copyright 2010-2011 10gen Inc.
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

namespace MongoDB.Driver.Wrappers {
    /// <summary>
    /// Represents a wrapped object that can be used where an IMongoUpdate is expected (the wrapped object is expected to serialize properly).
    /// </summary>
    public class UpdateWrapper : BaseWrapper, IMongoUpdate {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the UpdateWrapper class.
        /// </summary>
        /// <param name="update">The wrapped object.</param>
        public UpdateWrapper(
            object update
        )
            : base(update) {
        }

        /// <summary>
        /// Initializes a new instance of the UpdateWrapper class.
        /// </summary>
        /// <param name="nominalType">The nominal type of the wrapped object.</param>
        /// <param name="update">The wrapped object.</param>
        public UpdateWrapper(
            Type nominalType,
            object update
        )
            : base(nominalType, update) {
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Creates a new instance of the UpdateWrapper class (this overload is used when the update
        /// modifier is a replacement document).
        /// </summary>
        /// <typeparam name="T">The nominal type of the wrapped object.</typeparam>
        /// <param name="update">The wrapped object.</param>
        /// <returns>A new instance of UpdateWrapper or null.</returns>
        public static UpdateWrapper Create<T>(
            T update
        ) {
            if (update == null) {
                return null;
            } else {
                return new UpdateWrapper(typeof(T), update);
            }
        }

        /// <summary>
        /// Creates a new instance of the UpdateWrapper class.
        /// </summary>
        /// <param name="update">The wrapped object.</param>
        /// <returns>A new instance of UpdateWrapper or null.</returns>
        public static UpdateWrapper Create(
            object update
        ) {
            if (update == null) {
                return null;
            } else {
                return new UpdateWrapper(update);
            }
        }
        #endregion
    }
}
