/* Copyright 2021-present MongoDB Inc.
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

using MongoDB.Bson;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Defines the time series options to use when creating a time-series collection.
    /// </summary>
    public class TimeSeriesOptions
    {
        private readonly string _timeField;
        private readonly string _metaField;
        private readonly TimeSeriesGranularity? _granularity;

        /// <summary>
        ///
        /// </summary>
        /// <param name="timeField"></param>
        /// <param name="metaField"></param>
        /// <param name="granularity"></param>
        public TimeSeriesOptions(string timeField, string metaField, TimeSeriesGranularity? granularity)
        {
            _timeField = timeField;
            _metaField = metaField;
            _granularity = granularity;
        }

        /// <summary>
        /// The name of the field which contains the date in each time-series document.
        /// </summary>
        public string TimeField => _timeField;

        /// <summary>
        ///  The name of the field which contains metadata in each time-series document.
        /// </summary>
        public string MetaField => _metaField;

        /// <summary>
        /// The coarse granularity of time-series data.
        /// </summary>
        public TimeSeriesGranularity? Granularity => _granularity;

        /// <summary>
        /// The BSON representation of the time-series options.
        /// </summary>
        /// <returns></returns>
        public BsonDocument ToBsonDocument()
        {
            return new BsonDocument
            {
                { "timeField", _timeField },
                { "metaField", _metaField, _metaField != null },
                { "granularity", _granularity.Value.ToString().ToLowerInvariant(), _granularity.HasValue }
            };
        }
    }
}
