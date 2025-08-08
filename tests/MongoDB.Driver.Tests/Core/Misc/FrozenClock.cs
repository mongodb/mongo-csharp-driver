/* Copyright 2013-present MongoDB Inc.
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
using Moq;

namespace MongoDB.Driver.Core.Misc
{
    internal class FrozenClock : IClock
    {
        // public static methods
        public static FrozenClock FreezeUtcNow()
        {
            return new FrozenClock(DateTime.UtcNow);
        }

        // private fields
        private DateTime _utcNow;

        // constructors
        public FrozenClock(DateTime utcNow)
        {
            _utcNow = utcNow;
        }

        // public properties
        public DateTime UtcNow
        {
            get { return _utcNow; }
        }

        public void AdvanceCurrentTime(TimeSpan timeSpan)
        {
            _utcNow += timeSpan;
        }

        public IStopwatch StartStopwatch()
        {
            var startTime = _utcNow;
            var mockStopwatch = new Mock<IStopwatch>();
            mockStopwatch.SetupGet(w => w.Elapsed).Returns(() => _utcNow - startTime);
            return mockStopwatch.Object;
        }
    }
}
