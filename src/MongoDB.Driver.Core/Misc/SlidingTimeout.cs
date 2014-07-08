/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Misc
{
    public class SlidingTimeout
    {
        #region static
        // static operators
        public static implicit operator TimeSpan(SlidingTimeout slidingTimeout)
        {
            return slidingTimeout.ToTimeout();
        }
        #endregion

        // fields
        private readonly DateTime _expiration;

        // constructors
        public SlidingTimeout(TimeSpan timeout)
        {
            if (timeout == TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan)
            {
                _expiration = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            }
            else
            {
                _expiration = DateTime.UtcNow.Add(timeout);
            }
        }

        // properties
        public DateTime Expiration
        {
            get { return _expiration; }
        }

        // methods
        public void ThrowIfExpired()
        {
            if (DateTime.UtcNow > _expiration)
            {
                throw new TimeoutException();
            }
        }

        public TimeSpan ToTimeout()
        {
            if (_expiration == DateTime.MaxValue)
            {
                return Timeout.InfiniteTimeSpan;
            }
            else
            {
                var timeout = _expiration.Subtract(DateTime.UtcNow);
                if (timeout < TimeSpan.Zero)
                {
                    throw new TimeoutException();
                }
                return timeout;
            }
        }
    }
}
