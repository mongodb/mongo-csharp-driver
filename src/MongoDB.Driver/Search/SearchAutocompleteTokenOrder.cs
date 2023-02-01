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
    /// The order in which to search for tokens in an autocomplete search definition.
    /// </summary>
    public enum SearchAutocompleteTokenOrder
    {
        /// <summary>
        /// Indicates that tokens in the query can appear in any order in the documents.
        /// </summary>
        Any,
        
        /// <summary>
        /// Indicates that tokens in the query must appear adjacent to each other or in the order
        /// specified in the query in the documents.
        /// </summary>
        Sequential
    }
}
