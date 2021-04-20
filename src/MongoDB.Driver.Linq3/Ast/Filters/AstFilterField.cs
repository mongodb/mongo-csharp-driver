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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq3.Ast.Filters
{
    internal sealed class AstFilterField : AstNode
    {
        private string _path;
        private IBsonSerializer _serializer;

        public AstFilterField(string path, IBsonSerializer serializer)
        {
            _path = Ensure.IsNotNull(path, nameof(path));
            _serializer = Ensure.IsNotNull(serializer, nameof(serializer));
        }

        public string Path => _path;
        public override AstNodeType NodeType => AstNodeType.FilterField;
        public IBsonSerializer Serializer => _serializer;

        public AstFilterField SubField(string subFieldName, IBsonSerializer subFieldSerializer)
        {
            Ensure.IsNotNull(subFieldName, nameof(subFieldName));

            if (_path == "$CURRENT")
            {
                return new AstFilterField(subFieldName, subFieldSerializer);
            }
            else
            {
                return new AstFilterField(_path + "." + subFieldName, subFieldSerializer);
            }
        }

        public override BsonValue Render() => _path;
    }
}
