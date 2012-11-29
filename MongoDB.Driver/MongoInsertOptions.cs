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
using System.IO;
using System.Linq;
using System.Text;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the options to use for an Insert or InsertBatch operation.
    /// </summary>
    public class MongoInsertOptions
    {
        // private fields
        private bool _checkElementNames;
        private InsertFlags _flags;
        private WriteConcern _writeConcern;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoInsertOptions class.
        /// </summary>
        public MongoInsertOptions()
        {
            _checkElementNames = true;
            _flags = InsertFlags.None;
        }

        /// <summary>
        /// Initializes a new instance of the MongoInsertOptions class.
        /// </summary>
        /// <param name="collection">The collection from which to get default settings for the options.</param>
        [Obsolete("Options constructors which take a MongoCollection parameter are obsolete and will be removed in a future release of the MongoDB CSharp Driver. Please use a constructor which does not take a MongoCollection parameter.")]
        public MongoInsertOptions(MongoCollection collection) : this()
        {
            _writeConcern = collection.Settings.WriteConcern;
        }

        // public properties
        /// <summary>
        /// Gets or sets whether to check element names before proceeding with the Insert.
        /// </summary>
        public bool CheckElementNames
        {
            get { return _checkElementNames; }
            set { _checkElementNames = value; }
        }

        /// <summary>
        /// Gets or sets the insert flags.
        /// </summary>
        public InsertFlags Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }

        /// <summary>
        /// Gets or sets the SafeMode to use for the Insert.
        /// </summary>
        [Obsolete("Use WriteConcern instead.")]
        public SafeMode SafeMode
        {
            get { return (_writeConcern == null) ? null : new SafeMode(_writeConcern); }
            set { _writeConcern = (value == null) ? null : value.WriteConcern; }
        }

        /// <summary>
        /// Gets or sets the WriteConcern to use for the Insert.
        /// </summary>
        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }
    }
}
