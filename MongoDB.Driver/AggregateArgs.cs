﻿/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the output mode for an aggregate operation.
    /// </summary>
    public enum AggregateOutputMode
    {
        /// <summary>
        /// The output of the aggregate operation is returned inline.
        /// </summary>
        Inline,
        /// <summary>
        /// The output of the aggregate operation is returned using a cursor.
        /// </summary>
        Cursor
    }

    /// <summary>
    /// Represents options for the Aggregate command.
    /// </summary>
    public class AggregateArgs
    {
        // private fields
        private bool _allowDiskUsage;
        private int? _batchSize;
        private TimeSpan? _maxTime;
        private AggregateOutputMode _outputMode = AggregateOutputMode.Inline;
        private IEnumerable<BsonDocument> _pipeline;

        // public properties
        /// <summary>
        /// Gets or sets a value indicating whether disk usage is allowed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if disk usage is allowed; otherwise, <c>false</c>.
        /// </value>
        public bool AllowDiskUsage
        {
            get { return _allowDiskUsage; }
            set { _allowDiskUsage = value; }
        }

        /// <summary>
        /// Gets or sets the size of a batch when using a cursor.
        /// </summary>
        /// <value>
        /// The size of a batch.
        /// </value>
        /// <exception cref="System.ArgumentException">BatchSize cannot be negative.;value</exception>
        public int? BatchSize
        {
            get { return _batchSize; }
            set
            {
                if (value.HasValue && value.Value < 0)
                {
                    throw new ArgumentException("BatchSize cannot be negative.", "value");
                }
                _batchSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the max time the server should spend on the aggregation command.
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
        /// Gets or sets the output mode.
        /// </summary>
        /// <value>
        /// The output mode.
        /// </value>
        /// <exception cref="System.ArgumentException"></exception>
        public AggregateOutputMode OutputMode
        {
            get { return _outputMode; }
            set { _outputMode = value; }
        }

        /// <summary>
        /// Gets or sets the pipeline.
        /// </summary>
        /// <value>
        /// The pipeline.
        /// </value>
        public IEnumerable<BsonDocument> Pipeline
        {
            get { return _pipeline; }
            set { _pipeline = value.ToList(); }
        }
    }
}
