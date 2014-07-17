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
using MongoDB.Driver.Core.Authentication.Credentials;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Configuration
{
    public class ConnectionSettings
    {
        #region static
        // static fields
        private static readonly IReadOnlyList<ICredential> __noCredentials = new ICredential[0];
        #endregion

        // fields
        private readonly IReadOnlyList<ICredential> _credentials = __noCredentials;

        // constructors
        public ConnectionSettings()
        {
        }

        private ConnectionSettings(
            IReadOnlyList<ICredential> credentials)
        {
            _credentials = credentials;
        }

        // properties
        public IReadOnlyList<ICredential> Credentials
        {
            get { return _credentials; }
        }

        // methods
        public ConnectionSettings WithCredentials(IEnumerable<ICredential> value)
        {
            Ensure.IsNotNull(value, "value");

            if (object.ReferenceEquals(_credentials, value))
            {
                return this;
            }

            return _credentials.SequenceEqual(value) ? this : new ConnectionSettings(value.ToList());
        }
    }
}
