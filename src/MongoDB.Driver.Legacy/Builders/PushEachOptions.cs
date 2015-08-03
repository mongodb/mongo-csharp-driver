/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Driver;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// Arguments for $push with an $each clause.
    /// </summary>
    public class PushEachOptions
    {
        // private fields
        private int? _position;
        private int? _slice;
        private IMongoSortBy _sort;

        // public properties
        /// <summary>
        /// Gets or sets the position (see $position).
        /// </summary>
        public int? Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// Gets or sets the slice (see $slice).
        /// </summary>
        public int? Slice
        {
            get { return _slice; }
            set { _slice = value; }
        }

        /// <summary>
        /// Gets or sets the sort (see $sort).
        /// </summary>
        public IMongoSortBy Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }
    }

    /// <summary>
    /// A fluent builder for PushEachOptions.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class PushEachOptionsBuilder<TValue>
    {
        // private fields
        private readonly BsonSerializationInfoHelper _serializationInfoHelper;
        private int? _position;
        private SortByBuilder<TValue> _sortBy;
        private int? _slice;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PushEachOptionsBuilder{TValue}" /> class.
        /// </summary>
        /// <param name="serializationInfoHelper">The serialization info helper.</param>
        internal PushEachOptionsBuilder(BsonSerializationInfoHelper serializationInfoHelper)
        {
            _serializationInfoHelper = serializationInfoHelper;
        }

        // public methods
        /// <summary>
        /// Specifies the position for the array.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>The builder.</returns>
        public PushEachOptionsBuilder<TValue> Position(int position)
        {
            _position = position;
            return this;
        }

        /// <summary>
        /// Specifies a slice for the array.
        /// </summary>
        /// <param name="slice">The slice.</param>
        /// <returns>The builder.</returns>
        public PushEachOptionsBuilder<TValue> Slice(int slice)
        {
            _slice = slice;
            return this;
        }

        /// <summary>
        /// Sorts the array in ascending order.
        /// </summary>
        /// <param name="memberExpressions">The member expressions.</param>
        /// <returns>The builder.</returns>
        public PushEachOptionsBuilder<TValue> SortAscending(params Expression<Func<TValue, object>>[] memberExpressions)
        {
            if (_sortBy == null)
            {
                _sortBy = new SortByBuilder<TValue>(_serializationInfoHelper);
            }

            _sortBy.Ascending(memberExpressions);
            return this;
        }

        /// <summary>
        /// Sorts the array in descending order.
        /// </summary>
        /// <param name="memberExpressions">The member expressions.</param>
        /// <returns>The builder.</returns>
        public PushEachOptionsBuilder<TValue> SortDescending(params Expression<Func<TValue, object>>[] memberExpressions)
        {
            if (_sortBy == null)
            {
                _sortBy = new SortByBuilder<TValue>(_serializationInfoHelper);
            }

            _sortBy.Descending(memberExpressions);
            return this;
        }

        /// <summary>
        /// Builds the PushEachArgs.
        /// </summary>
        /// <returns>A built PushEachOptions.</returns>
        public PushEachOptions Build()
        {
            return new PushEachOptions
            {
                Position = _position,
                Slice = _slice,
                Sort = _sortBy
            };
        }
    }
}