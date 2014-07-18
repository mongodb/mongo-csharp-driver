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

        // constructors
        public ConnectionSettings()
        {
        }

        private ConnectionSettings(IReadOnlyList<IAuthenticator> authenticators)
        {
            _authenticators = authenticators;
        }

        // properties
        public IReadOnlyList<IAuthenticator> Authenticators
        {
            get { return _authenticators; }
        }

        // methods
        public ConnectionSettings WithAuthenticators(IEnumerable<IAuthenticator> value)
        {
            Ensure.IsNotNull(value, "value");

            if (object.ReferenceEquals(_authenticators, value))
            {
                return this;
            }

            return _authenticators.SequenceEqual(value) ? this : new ConnectionSettings(value.ToList());
        }
    }
}
