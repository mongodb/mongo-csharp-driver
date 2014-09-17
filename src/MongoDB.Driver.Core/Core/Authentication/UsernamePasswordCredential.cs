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

using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication
{
    public class UsernamePasswordCredential
    {
        // fields
        private string _source;
        private string _password;
        private string _username;

        // constructors
        public UsernamePasswordCredential(string source, string username, string password)
        {
            _source = Ensure.IsNotNullOrEmpty(source, "source");
            _username = Ensure.IsNotNullOrEmpty(username, "username");
            _password = Ensure.IsNotNull(password, "password");
        }

        // properties
        public string Password
        {
            get { return _password; }
        }

        public string Source
        {
            get { return _source; }
        }

        public string Username
        {
            get { return _username; }
        }
    }
}
