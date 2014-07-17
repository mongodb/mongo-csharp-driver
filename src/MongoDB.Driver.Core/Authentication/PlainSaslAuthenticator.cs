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

using System.Text;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication
{
    public class PlainSaslAuthenticator : SaslAuthenticator
    {
        // fields
        private readonly string _databaseName;

        // constructors
        public PlainSaslAuthenticator(UsernamePasswordCredential credential)
            : base(new PlainMechanism(credential))
        {
            _databaseName = credential.Source;
        }

        // properties
        public override string DatabaseName
        {
            get { return _databaseName; }
        }

        // nested classes
        private class PlainMechanism : ISaslMechanism
        {
            // fields
            private readonly UsernamePasswordCredential _credential;

            // constructors
            public PlainMechanism(UsernamePasswordCredential credential)
            {
                _credential = Ensure.IsNotNull(credential, "credential");
            }

            // properties
            public string Name
            {
                get { return "PLAIN"; }
            }

            // methods
            public ISaslStep Initialize(IRootConnection connection)
            {
                var dataString = string.Format("\0{0}\0{1}",
                    _credential.Username,
                    _credential.Password);

                var bytes = new UTF8Encoding(false, true).GetBytes(dataString);
                return new CompleteAfterTransitionStep(bytes);
            }
        }
    }
}