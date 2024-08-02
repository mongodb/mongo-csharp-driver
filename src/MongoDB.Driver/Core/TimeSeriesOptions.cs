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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Defines the time series options to use when creating a time series collection. See https://www.mongodb.com/docs/manual/reference/command/create/ for supported options
    /// and https://www.mongodb.com/docs/manual/core/timeseries-collections/ for more information on time series collections.
    /// </summary>
    public class TimeSeriesOptions
    {
        private readonly TimeSeriesGranularity? _granularity;
        private readonly string _metaField;
        private readonly string _timeField;
        private readonly int? _bucketMaxSpanSeconds;
        private readonly int? _bucketRoundingSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSeriesOptions"/> class.
        /// </summary>
        /// <param name="timeField">The name of the top-level field to be used for time.</param>
        /// <param name="metaField">The name of the top-level field describing the series upon which related data will be grouped.</param>
        /// <param name="granularity">The <see cref="TimeSeriesGranularity"/> for the time series. Do not set if using bucketMaxSpanSeconds</param>
        /// <param name="bucketMaxSpanSeconds">The maximum time between timestamps in the same bucket.</param>
        /// <param name="bucketRoundingSeconds">The interval used to round down the first timestamp when opening a new bucket.</param>
        public TimeSeriesOptions(string timeField, Optional<string> metaField = default, Optional<TimeSeriesGranularity?> granularity = default, Optional<int?> bucketMaxSpanSeconds = default, Optional<int?> bucketRoundingSeconds = default)
        {
            _timeField = Ensure.IsNotNullOrEmpty(timeField, nameof(timeField));
            _metaField = metaField.WithDefault(null);
            _granularity = granularity.WithDefault(null);
            _bucketMaxSpanSeconds = bucketMaxSpanSeconds.WithDefault(null);
            _bucketRoundingSeconds = bucketRoundingSeconds.WithDefault(null);
        }

        /// <summary>
        /// The coarse granularity of time series data.
        /// </summary>
        public TimeSeriesGranularity? Granularity => _granularity;

        /// <summary>
        /// The name of the field which contains metadata in each time series document.
        /// </summary>
        public string MetaField => _metaField;

        /// <summary>
        /// The name of the field which contains the date and time in each time series document.
        /// </summary>
        public string TimeField => _timeField;

        /// <summary>
        /// The maximum time between timestamps in the same bucket.
        /// </summary>
        public int? BucketMaxSpanSeconds => _bucketMaxSpanSeconds;

        /// <summary>
        /// The interval used to round down the first timestamp when opening a new bucket.
        /// </summary>
        public int? BucketRoundingSeconds => _bucketRoundingSeconds;

        /// <summary>
        /// The BSON representation of the time series options.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public BsonDocument ToBsonDocument()
        {
            return new BsonDocument
            {
                { "timeField", _timeField },
                { "metaField", _metaField, _metaField != null },
                { "granularity", () => _granularity.Value.ToString().ToLowerInvariant(), _granularity.HasValue },
                { "bucketMaxSpanSeconds", _bucketMaxSpanSeconds, _bucketMaxSpanSeconds != null },
                { "bucketRoundingSeconds", _bucketRoundingSeconds, _bucketRoundingSeconds != null }
            };
        }
    }
}
