/* Copyright 2010-present MongoDB Inc.
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

using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq3.Misc
{
    public class Symbol
    {
        // private fields
        private readonly string _name;
        private readonly IBsonSerializer _serializer;

        // constructors
        public Symbol(string name, IBsonSerializer serializer)
        {
            _name = Ensure.IsNotNullOrEmpty(name, nameof(name));
            _serializer = Ensure.IsNotNull(serializer, nameof(serializer));
        }

        // public properties
        public string Name => _name;

        public IBsonSerializer Serializer => _serializer;

        // public methods
        public override string ToString() => $"\"{_name}\"";
    }
}
