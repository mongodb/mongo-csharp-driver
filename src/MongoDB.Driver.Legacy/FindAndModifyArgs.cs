/* Copyright 2010-2016 MongoDB Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a version of a document (original or modified).
    /// </summary>
    public enum FindAndModifyDocumentVersion
    {
        /// <summary>
        /// The original version of the document.
        /// </summary>
        Original = 0,
        /// <summary>
        /// The modified version of the document.
        /// </summary>
        Modified
    }

    /// <summary>
    /// Represents options for the FindAndModify command.
    /// </summary>
    public class FindAndModifyArgs
    {
        // private fields
        private bool? _bypassDocumentValidation;
        private Collation _collation;
        private IMongoFields _fields;
        private TimeSpan? _maxTime;
        private IMongoQuery _query;
        private IMongoSortBy _sort;
        private IMongoUpdate _update;
        private bool _upsert;
        private FindAndModifyDocumentVersion? _versionReturned;

        // public properties
        /// <summary>
        /// Gets or sets a value indicating whether to bypass document validation.
        /// </summary>
        /// <value>
        /// A value indicating whether to bypass document validation.
        /// </value>
        public bool? BypassDocumentValidation
        {
            get { return _bypassDocumentValidation; }
            set { _bypassDocumentValidation = value; }
        }

        /// <summary>
        /// Gets or sets the collation.
        /// </summary>
        /// <value>
        /// The collation.
        /// </value>
        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        /// <summary>
        /// Gets or sets the fields specification.
        /// </summary>
        public IMongoFields Fields
        {
            get { return _fields; }
            set { _fields = value; }
        }

        /// <summary>
        /// Gets or sets the max time.
        /// </summary>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        public IMongoQuery Query
        {
            get { return _query; }
            set { _query = value; }
        }


        /// <summary>
        /// Gets or sets the sort specification.
        /// </summary>
        public IMongoSortBy SortBy
        {
            get { return _sort; }
            set { _sort = value; }
        }

        /// <summary>
        /// Gets or sets the update specification.
        /// </summary>
        public IMongoUpdate Update
        {
            get { return _update; }
            set { _update = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether FindAndModify should upsert.
        /// </summary>
        public bool Upsert
        {
            get { return _upsert; }
            set { _upsert = value; }
        }

        /// <summary>
        /// Gets or sets the version of the document returned.
        /// </summary>
        public FindAndModifyDocumentVersion? VersionReturned
        {
            get { return _versionReturned; }
            set { _versionReturned = value; }
        }
    }
}
