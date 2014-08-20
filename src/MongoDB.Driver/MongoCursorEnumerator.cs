/* Copyright 2010-2014 MongoDB Inc.
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
    /// Reprsents an enumerator that fetches the results of a query sent to the server.
    /// </summary>
    /// <typeparam name="TDocument">The type of the documents returned.</typeparam>
    public class MongoCursorEnumerator<TDocument> : IEnumerator<TDocument>
    {
        // private fields
        private readonly MongoCursor<TDocument> _cursor;
        private readonly ReadPreference _readPreference;

        private bool _disposed = false;
        
        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoCursorEnumerator class.
        /// </summary>
        /// <param name="cursor">The cursor to be enumerated.</param>
        public MongoCursorEnumerator(MongoCursor<TDocument> cursor)
        {
            _cursor = cursor;
            _readPreference = _cursor.ReadPreference;
        }

        // public properties
        /// <summary>
        /// Gets the current document.
        /// </summary>
        public TDocument Current
        {
            get
            {
                if (_disposed) { throw new ObjectDisposedException("MongoCursorEnumerator"); }
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets whether the cursor is dead (used with tailable cursors).
        /// </summary>
        public bool IsDead
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets whether the server is await capable (used with tailable cursors).
        /// </summary>
        public bool IsServerAwaitCapable
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        // public methods
        /// <summary>
        /// Disposes of any resources held by this enumerator.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Moves to the next result and returns true if another result is available.
        /// </summary>
        /// <returns>True if another result is available.</returns>
        public bool MoveNext()
        {
            if (_disposed) { throw new ObjectDisposedException("MongoCursorEnumerator"); }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Resets the enumerator (not supported by MongoCursorEnumerator).
        /// </summary>
        public void Reset()
        {
            throw new NotSupportedException();
        }

        // explicit interface implementations
        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}
