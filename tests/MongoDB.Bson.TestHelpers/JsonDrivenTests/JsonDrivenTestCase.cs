/* Copyright 2018-present MongoDB Inc.
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

using Xunit.Abstractions;

namespace MongoDB.Bson.TestHelpers.JsonDrivenTests
{
    public class JsonDrivenTestCase : IXunitSerializable
    {
        // private fields
        private string _name;
        private BsonDocument _shared;
        private BsonDocument _test;

        // public constructors
        public JsonDrivenTestCase()
        {
        }

        public JsonDrivenTestCase(string name, BsonDocument shared, BsonDocument test)
        {
            _name = name;
            _shared = shared;
            _test = test;
        }

        // public properties
        public string Name => _name;

        public BsonDocument Shared => _shared;

        public BsonDocument Test => _test;

        // public methods
        public void Deserialize(IXunitSerializationInfo info)
        {
            _name = info.GetValue<string>(nameof(_name));
            _shared = BsonDocument.Parse(info.GetValue<string>(nameof(_shared)));
            _test = BsonDocument.Parse(info.GetValue<string>(nameof(_test)));
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(_name), _name);
            info.AddValue(nameof(_shared), _shared.ToJson());
            info.AddValue(nameof(_test), _shared.ToJson());
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
