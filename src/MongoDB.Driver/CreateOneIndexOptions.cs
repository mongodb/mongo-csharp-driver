﻿/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for creating a single index.
    /// </summary>
    public class CreateOneIndexOptions
    {
        // private fields
        private CreateIndexCommitQuorum _commitQuorum;
        private TimeSpan? _maxTime;
        private TimeSpan? _timeout;

        // public properties
        /// <summary>
        /// Gets or sets the commit quorum.
        /// </summary>
        /// <value>The commit quorum.</value>
        public CreateIndexCommitQuorum CommitQuorum
        {
            get { return _commitQuorum; }
            set { _commitQuorum = value; }
        }

        /// <summary>
        /// Gets or sets the maximum time.
        /// </summary>
        /// <value>The maximum time.</value>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        /// <summary>
        /// Gets or sets the operation timeout.
        /// </summary>
        // TODO: CSOT: Make it public when CSOT will be ready for GA
        internal TimeSpan? Timeout
        {
            get => _timeout;
            set => _timeout = Ensure.IsNullOrValidTimeout(value, nameof(Timeout));
        }
    }
}
