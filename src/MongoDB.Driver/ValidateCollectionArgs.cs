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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents arguments for the Validate collection command helper method.
    /// </summary>
    public class ValidateCollectionArgs
    {
        // private fields
        private bool? _full;
        private TimeSpan? _maxTime;
        private bool? _scanData;

        // public properties
        /// <summary>
        /// Gets or sets whether to do a more thorough scan of the data.
        /// </summary>
        /// <value>
        /// Whether to do a more thorough scan of the data.
        /// </value>
        public bool? Full
        {
            get { return _full; }
            set { _full = value; }
        }

        /// <summary>
        /// Gets or sets the max time.
        /// </summary>
        /// <value>
        /// The max time.
        /// </value>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        /// <summary>
        /// Gets or sets whether to scan the data.
        /// </summary>
        /// <value>
        /// Whether to scan the data.
        /// </value>
        public bool? ScanData
        {
            get { return _scanData; }
            set { _scanData = value; }
        }
    }
}
