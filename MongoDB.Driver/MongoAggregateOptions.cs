/* Copyright 2010-2013 10gen Inc.
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
    public class MongoAggregateOptions
    {
        // private fields
        private bool _allowDiskUsage;
        private int _batchSize = -1;
        private AggregateOutputMode _outputMode = AggregateOutputMode.Inline;

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
        /// Gets or sets the size of the batch.
        /// </summary>
        /// <value>
        /// The size of the batch.
        /// </value>
        /// <exception cref="System.ArgumentException">Invalid batch size.;value</exception>
        public int BatchSize
        {
            get { return _batchSize; }
            set
            {
                if (value < -1)
                {
                    throw new ArgumentException("Invalid batch size.", "value");
                }
                _batchSize = value;
            }
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
    }
}
