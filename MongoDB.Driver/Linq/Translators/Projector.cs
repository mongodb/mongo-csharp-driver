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
using System.Linq;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Represents a projection from TSource to TResult;
    /// </summary>
    internal interface IProjector : IEnumerable
    {
        /// <summary>
        /// Gets the cursor.
        /// </summary>
        MongoCursor Cursor { get; }
    }

    /// <summary>
    /// Represents a projection from TSource to TResult;
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    internal interface IProjector<TSource, TResult> : IProjector, IEnumerable<TResult>
    { }

    /// <summary>
    /// Represents a projector that does nothing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class IdentityProjector<T> : IProjector<T,T>
    {
        // private fields
        private readonly MongoCursor _cursor;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityProjector&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="cursor">The cursor.</param>
        public IdentityProjector(MongoCursor cursor)
        {
            _cursor = cursor;
        }

        // public properties
        /// <summary>
        /// Gets the cursor.
        /// </summary>
        public MongoCursor Cursor
        {
            get { return _cursor; }
        }

        // public methods
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_cursor).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Represents a projection.
    /// </summary>
    /// <typeparam name="TSource">The type of the source objects.</typeparam>
    /// <typeparam name="TResult">The type of the result objects.</typeparam>
    internal class Projector<TSource, TResult> : IProjector<TSource, TResult>
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

        // public properties
        /// <summary>
        /// Gets the cursor.
        /// </summary>
        public MongoCursor Cursor
        {
            get { return _cursor; }
        }

        // public methods
        /// <summary>
        /// Gets an enumerator for the result objects.
        /// </summary>
        /// <returns>An enumerator for the result objects.</returns>
        public IEnumerator<TResult> GetEnumerator()
        {
            return _cursor.Select(_projection).GetEnumerator();
        }

        // explicit interface implementation
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
