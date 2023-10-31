/* Copyright 2010-present MongoDB Inc.
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

using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Change stream splitEvent data.
    /// </summary>
    public sealed class ChangeStreamSplitEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeStreamSplitEvent" /> class.
        /// </summary>
        /// <param name="fragment">Fragment index.</param>
        /// <param name="of">Total number of fragments.</param>
        public ChangeStreamSplitEvent(int fragment, int of)
        {
            Fragment = Ensure.IsGreaterThanZero(fragment, nameof(of));
            Of = Ensure.IsGreaterThanZero(of, nameof(of));
        }

        /// <summary>
        /// Gets the fragment index, starting at 1.
        /// </summary>
        public int Fragment { get; }

        /// <summary>
        /// Total number of fragments for the event.
        /// </summary>
        public int Of { get; }
    }
}
