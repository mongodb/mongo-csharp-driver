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
        private readonly IReadOnlyList<IAuthenticator> _authenticators = __noAuthenticators;
        private readonly TimeSpan _maxIdleTime;
        private readonly TimeSpan _maxLifeTime;

        // constructors
        public ConnectionSettings()
        {
            _authenticators = new List<IAuthenticator>();
            _maxIdleTime = TimeSpan.FromMinutes(10);
            _maxLifeTime = TimeSpan.FromMinutes(30);
        }

        private ConnectionSettings(
            IReadOnlyList<IAuthenticator> authenticators,
            TimeSpan maxIdleTime,
            TimeSpan maxLifeTime)
        {
            _authenticators = authenticators;
            _maxIdleTime = maxIdleTime;
            _maxLifeTime = maxLifeTime;
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
        public ConnectionSettings WithAuthenticators(IEnumerable<IAuthenticator> value)
        {
            Ensure.IsNotNull(value, "value");

            if (object.ReferenceEquals(_authenticators, value))
            {
                return this;
            }

            return _authenticators.SequenceEqual(value) ? this : new Builder(this) { _authenticators = value.ToList() }.Build();
        }

        public ConnectionSettings WithMaxIdleTime(TimeSpan value)
        {
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero(value, "value");
            return (_maxIdleTime == value) ? this : new Builder(this) { _maxIdleTime = value }.Build();
        }

        public ConnectionSettings WithMaxLifeTime(TimeSpan value)
        {
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero(value, "value");
            return (_maxLifeTime == value) ? this : new Builder(this) { _maxLifeTime = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public IReadOnlyList<IAuthenticator> _authenticators;
            public TimeSpan _maxIdleTime;
            public TimeSpan _maxLifeTime;

            // constructors
            public Builder(ConnectionSettings other)
            {
                _authenticators = other._authenticators;
                _maxIdleTime = other._maxIdleTime;
                _maxLifeTime = other._maxLifeTime;
            }

            // methods
            public ConnectionSettings Build()
            {
                return new ConnectionSettings(
                    _authenticators,
                    _maxIdleTime,
                    _maxLifeTime);
            }
        }
    }
}
