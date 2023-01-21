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

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// The type of count of the documents in a search result set.
    /// </summary>
    public enum SearchCountType
    {
        /// <summary>
        /// A lower bound count of the number of documents that match the query.
        /// </summary>
        LowerBound,

        /// <summary>
        /// An exact count of the number of documents that match the query.
        /// </summary>
        Total
    }
}
