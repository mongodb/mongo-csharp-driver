/* Copyright 2016 MongoDB Inc.
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

using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for the aggregate $facet stage.
    /// </summary>
    /// <typeparam name="TNewResult">The type of the new result.</typeparam>
    public sealed class AggregateFacetOptions<TNewResult>
    {
        private IBsonSerializer<TNewResult> _newResultSerializer;

        /// <summary>
        /// Gets or sets the new result serializer.
        /// </summary>
        public IBsonSerializer<TNewResult> NewResultSerializer
        {
            get { return _newResultSerializer; }
            set { _newResultSerializer = value; }
        }
    }
}
