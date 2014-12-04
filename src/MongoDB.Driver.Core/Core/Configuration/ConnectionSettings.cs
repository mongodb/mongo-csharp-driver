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
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Configuration
{
    public class ConnectionSettings
    {
        #region static
        // static fields
        private static readonly IEqualityComparer<IEnumerable<IAuthenticator>> __authenticatorsComparer = new AuthenticatorsComparer();
        private static readonly IReadOnlyList<IAuthenticator> __noAuthenticators = new IAuthenticator[0];
        #endregion

        // fields
        private readonly IReadOnlyList<IAuthenticator> _authenticators = __noAuthenticators;
        private readonly TimeSpan _maxIdleTime;
        private readonly TimeSpan _maxLifeTime;

        // constructors
        public ConnectionSettings(
            Optional<IEnumerable<IAuthenticator>> authenticators = default(Optional<IEnumerable<IAuthenticator>>),
            Optional<TimeSpan> maxIdleTime = default(Optional<TimeSpan>),
            Optional<TimeSpan> maxLifeTime = default(Optional<TimeSpan>))
        {
            _authenticators = authenticators.HasValue && authenticators.Value != null ? authenticators.Value.ToList() : __noAuthenticators;
            _maxIdleTime = Ensure.IsGreaterThanZero(maxIdleTime.WithDefault(TimeSpan.FromMinutes(10)), "maxIdleTime");
            _maxLifeTime = Ensure.IsGreaterThanZero(maxLifeTime.WithDefault(TimeSpan.FromMinutes(30)), "maxLifeTime");
        }

        // properties
        public IReadOnlyList<IAuthenticator> Authenticators
        {
            get { return _authenticators; }
        }

        public TimeSpan MaxIdleTime
        {
            get { return _maxIdleTime; }
        }

        public TimeSpan MaxLifeTime
        {
            get { return _maxLifeTime; }
        }

        // methods
        public ConnectionSettings With(
            Optional<IEnumerable<IAuthenticator>> authenticators = default(Optional<IEnumerable<IAuthenticator>>),
            Optional<TimeSpan> maxIdleTime = default(Optional<TimeSpan>),
            Optional<TimeSpan> maxLifeTime = default(Optional<TimeSpan>))
        {
            if (authenticators.Replaces(_authenticators, __authenticatorsComparer) ||
                maxIdleTime.Replaces(_maxIdleTime) ||
                maxLifeTime.Replaces(_maxLifeTime))
            {
                return new ConnectionSettings(
                    Optional.Arg(authenticators.WithDefault(_authenticators)),
                    maxIdleTime.WithDefault(_maxIdleTime),
                    maxLifeTime.WithDefault(_maxLifeTime));
            }
            else
            {
                return this;
            }
        }

        // nested types
        private class AuthenticatorsComparer : IEqualityComparer<IEnumerable<IAuthenticator>>
        {
            public bool Equals(IEnumerable<IAuthenticator> x, IEnumerable<IAuthenticator> y)
            {
                if (x == null) { return y == null; }
                return x.SequenceEqual(y);
            }

            public int GetHashCode(IEnumerable<IAuthenticator> x)
            {
                return 1;
            }
        }
    }
}
