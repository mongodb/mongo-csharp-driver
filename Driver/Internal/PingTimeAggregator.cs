/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Holds the ping times and a rolling calculated average.  This class is thread-safe.
    /// </summary>
    internal class PingTimeAggregator
    {
        // private fields
        private readonly object _lock = new object();
        private readonly int _maxCount;
        private readonly LinkedList<TimeSpan> _timespans;
        private TimeSpan _average;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PingTimeAggregator"/> class.
        /// </summary>
        /// <param name="maxCount">The max count.</param>
        public PingTimeAggregator(int maxCount)
        {
            _maxCount = maxCount;
            _timespans = new LinkedList<TimeSpan>();
            _average = TimeSpan.MaxValue;
        }

        // public properties
        /// <summary>
        /// Gets the average.
        /// </summary>
        public TimeSpan Average
        {
            get
            {
                lock (_lock)
                {
                    return _average;
                }
            }
        }

        // public methods
        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _timespans.Clear();
                _average = TimeSpan.MaxValue;
            }
        }

        /// <summary>
        /// Includes the specified ping time in the calculation.  If there are more times in the bucket than the maxCount, then the oldest one is replaced.
        /// </summary>
        /// <param name="pingTime">The ping time.</param>
        public void Include(TimeSpan pingTime)
        {
            lock (_lock)
            {
                _timespans.AddLast(pingTime);
                if (_timespans.Count >= _maxCount)
                {
                    _timespans.RemoveFirst();
                }

                _average = new TimeSpan((long)_timespans.Average(x => x.Ticks));
            }
        }
    }
}