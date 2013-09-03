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

using System;
using System.Collections;
using System.Collections.Generic;

namespace MongoDB.Driver
{
    /// <summary>
    /// Wraps an enumerator with an enumerable that can only be enumerated once.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public class EnumerableOneTimeWrapper<T> : IEnumerable<T>
    {
        // private fields
        private readonly IEnumerator<T> _enumerator;
        private readonly string _errorMessage;
        private bool _hasBeenEnumerated;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableOneTimeWrapper{T}"/> class.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="errorMessage">The error message that will be used if GetEnumerator is called more than once.</param>
        public EnumerableOneTimeWrapper(IEnumerator<T> enumerator, string errorMessage)
        {
            _enumerator = enumerator;
            _errorMessage = errorMessage;
        }

        // public methods
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public IEnumerator<T> GetEnumerator()
        {
            if (_hasBeenEnumerated)
            {
                throw new NotSupportedException(_errorMessage);
            }
            _hasBeenEnumerated = true;

            return _enumerator;
        }

        // explicitly implemented interfaces
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
