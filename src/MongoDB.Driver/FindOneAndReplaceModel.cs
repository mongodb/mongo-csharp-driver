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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Model for a findAndModify command to replace an object.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class FindOneAndReplaceModel<TDocument>
    {
        // fields
        private readonly object _criteria;
        private bool _isUpsert;
        private TimeSpan? _maxTime;
        private object _projection;
        private readonly TDocument _replacement;
        private bool _returnOriginal;
        private object _sort;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FindOneAndReplaceModel{TDocument}"/> class.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <param name="replacement">The replacement.</param>
        public FindOneAndReplaceModel(object criteria, TDocument replacement)
        {
            _criteria = Ensure.IsNotNull(criteria, "criteria");
            _replacement = replacement;
            _returnOriginal = true;
        }

        // properties
        /// <summary>
        /// Gets the criteria.
        /// </summary>
        public object Criteria
        {
            get { return _criteria; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [is upsert].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [is upsert]; otherwise, <c>false</c>.
        /// </value>
        public bool IsUpsert
        {
            get { return _isUpsert; }
            set { _isUpsert = value; }
        }

        /// <summary>
        /// Gets or sets the maximum time.
        /// </summary>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        /// <summary>
        /// Gets or sets the projection.
        /// </summary>
        public object Projection
        {
            get { return _projection; }
            set { _projection = value; }
        }

        /// <summary>
        /// Gets the replacement.
        /// </summary>
        public TDocument Replacement
        {
            get { return _replacement; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [return replaced].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [return replaced]; otherwise, <c>false</c>.
        /// </value>
        public bool ReturnOriginal
        {
            get { return _returnOriginal; }
            set { _returnOriginal = value; }
        }

        /// <summary>
        /// Gets or sets the sort.
        /// </summary>
        public object Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }
    }
}
