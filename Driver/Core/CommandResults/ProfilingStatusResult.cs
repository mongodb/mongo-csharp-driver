/* Copyright 2010-2011 10gen Inc.
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

namespace MongoDB.Driver {
    /// <summary>
    /// Represents the result of a GetProfilingStatus command.
    /// </summary>
    [Serializable]
    public class ProfilingStatusResult : CommandResult {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the DatabaseStatsResult class.
        /// </summary>
        public ProfilingStatusResult() {
        }
        #endregion

        #region public properties
        /// <summary>
        /// The duration threshold above which operations are logged.
        /// </summary>
        public int SlowThresholdMilliseconds {
            get { return response["slowms"].ToInt32(); }
        }

        /// <summary>
        /// The current profiling level.
        /// </summary>
        public MongoDatabaseProfilingLevel Level {
            get { return (MongoDatabaseProfilingLevel) response["was"].ToInt32(); }
        }
        #endregion
    }
}