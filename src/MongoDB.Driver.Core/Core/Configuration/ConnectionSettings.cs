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
        private static readonly IReadOnlyList<IAuthenticator> __noAuthenticators = new IAuthenticator[0];
        #endregion

        // fields
        private readonly IReadOnlyList<IAuthenticator> _authenticators;
        private readonly TimeSpan _maxIdleTime;
        private readonly TimeSpan _maxLifeTime;

        // constructors
        public ConnectionSettings(
            Optional<IEnumerable<IAuthenticator>> authenticators = default(Optional<IEnumerable<IAuthenticator>>),
            Optional<TimeSpan> maxIdleTime = default(Optional<TimeSpan>),
            Optional<TimeSpan> maxLifeTime = default(Optional<TimeSpan>))
        {
            _authenticators = Ensure.IsNotNull(authenticators.WithDefault(__noAuthenticators), "authenticators").ToList();
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
            return new ConnectionSettings(
                authenticators: Optional.Create(authenticators.WithDefault(_authenticators)),
                maxIdleTime: maxIdleTime.WithDefault(_maxIdleTime),
                maxLifeTime: maxLifeTime.WithDefault(_maxLifeTime));
        }
    }
}
