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
using System.Text;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Represents a projection.
    /// </summary>
    /// <typeparam name="TSource">The type of the source objects.</typeparam>
    /// <typeparam name="TResult">The type of the result objects.</typeparam>
    public class Projector<TSource, TResult> : IEnumerable<TResult>
    {
        // private fields
        private MongoCursor<TSource> _cursor;
        private Func<TSource, TResult> _projection;

        // constructors
        /// <summary>
        /// Initializes a new instance of the Projector class.
        /// </summary>
        /// <param name="cursor">The cursor that supplies the source objects.</param>
        /// <param name="projection">The projection.</param>
        public Projector(MongoCursor<TSource> cursor, Func<TSource, TResult> projection)
        {
            _cursor = cursor;
            _projection = projection;
        }

        // public methods
        /// <summary>
        /// Gets an enumerator for the result objects.
        /// </summary>
        /// <returns>An enumerator for the result objects.</returns>
        public IEnumerator<TResult> GetEnumerator()
        {
            foreach (var document in _cursor)
            {
                yield return _projection(document);
            }
        }

        // explicit interface implementation
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
