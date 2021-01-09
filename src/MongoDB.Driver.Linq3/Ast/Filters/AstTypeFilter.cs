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

using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq3.Ast.Filters
{
    public sealed class AstTypeFilter : AstFilter
    {
        private readonly AstFilterField _field;
        private readonly IReadOnlyList<BsonType> _types;

        public AstTypeFilter(AstFilterField field, BsonType type)
        {
            _field = Ensure.IsNotNull(field, nameof(field));
            _types = new List<BsonType> { type }.AsReadOnly();
        }

        public AstTypeFilter(AstFilterField field, IEnumerable<BsonType> types)
        {
            _field = Ensure.IsNotNull(field, nameof(field));
            _types = Ensure.IsNotNull(types, nameof(types)).ToList().AsReadOnly();
        }

        public AstFilterField Field => _field;
        public override AstNodeType NodeType => AstNodeType.TypeFilter;
        public BsonType Type => _types.Single();
        public IReadOnlyList<BsonType> Types => _types;

        public override BsonValue Render()
        {
            if (_types.Count == 1)
            {
                var type = _types[0];
                return new BsonDocument(_field.Path, new BsonDocument("$type", MapBsonTypeToString(type)));
            }
            else
            {
                return new BsonDocument(_field.Path, new BsonDocument("$type", new BsonArray(_types.Select(type => MapBsonTypeToString(type)))));
            }
        }

        private string MapBsonTypeToString(BsonType type)
        {
            switch (type)
            {
                case BsonType.Array: return "array";
                case BsonType.Binary: return "binData";
                case BsonType.Boolean: return "bool";
                case BsonType.DateTime: return "date";
                case BsonType.Decimal128: return "decimal";
                case BsonType.Document: return "object";
                case BsonType.Double: return "double";
                case BsonType.Int32: return "int";
                case BsonType.Int64: return "long";
                case BsonType.JavaScript: return "javascript";
                case BsonType.JavaScriptWithScope: return "javascriptWithScope";
                case BsonType.MaxKey: return "maxKey";
                case BsonType.MinKey: return "minKey";
                case BsonType.Null: return "null";
                case BsonType.ObjectId: return "objectId";
                case BsonType.RegularExpression: return "regex";
                case BsonType.String: return "string";
                case BsonType.Symbol: return "symbol";
                case BsonType.Timestamp: return "timestamp";
                case BsonType.Undefined: return "undefined";
                default: throw new ArgumentException($"Unexpected BSON type: {type}.", nameof(type));
            }
        }
    }
}
