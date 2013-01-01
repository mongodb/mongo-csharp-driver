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

namespace MongoDB.Driver.Wrappers
{
    /// <summary>
    /// Represents a wrapped object that can be used where an IMongoFields is expected (the wrapped object is expected to serialize properly).
    /// </summary>
    public class FieldsWrapper : BaseWrapper, IMongoFields
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the FieldsWrapper class.
        /// </summary>
        /// <param name="fields">The wrapped object.</param>
        public FieldsWrapper(object fields)
            : base(fields)
        {
        }

        // public static methods
        /// <summary>
        /// Creates a new instance of the FieldsWrapper class.
        /// </summary>
        /// <param name="fields">The wrapped object.</param>
        /// <returns>A new instance of FieldsWrapper or null.</returns>
        public static FieldsWrapper Create(object fields)
        {
            if (fields == null)
            {
                return null;
            }
            else
            {
                return new FieldsWrapper(fields);
            }
        }
    }
}
