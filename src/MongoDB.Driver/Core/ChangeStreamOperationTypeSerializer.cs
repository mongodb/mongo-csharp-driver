/* Copyright 2017-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver
{
    /// <summary>
    /// A serializer for ChangeStreamOperationType values.
    /// </summary>
    public class ChangeStreamOperationTypeSerializer : StructSerializerBase<ChangeStreamOperationType>
    {
        #region static
        // private static fields
        private static readonly ChangeStreamOperationTypeSerializer __instance = new ChangeStreamOperationTypeSerializer();

        // public static properties
        /// <summary>
        /// Gets a ChangeStreamOperationTypeSerializer.
        /// </summary>
        /// <value>
        /// A ChangeStreamOperationTypeSerializer.
        /// </value>
        public static ChangeStreamOperationTypeSerializer Instance => __instance;
        #endregion

        /// <inheritdoc />
        public override ChangeStreamOperationType Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            var stringValue = reader.ReadString();
            switch (stringValue)
            {
                case "delete": return ChangeStreamOperationType.Delete;
                case "insert": return ChangeStreamOperationType.Insert;
                case "invalidate": return ChangeStreamOperationType.Invalidate;
                case "replace": return ChangeStreamOperationType.Replace;
                case "update": return ChangeStreamOperationType.Update;
                case "rename": return ChangeStreamOperationType.Rename;
                case "drop": return ChangeStreamOperationType.Drop;
                case "dropDatabase": return ChangeStreamOperationType.DropDatabase;
                case "createIndexes": return ChangeStreamOperationType.CreateIndexes;
                case "dropIndexes": return ChangeStreamOperationType.DropIndexes;
                case "modify": return ChangeStreamOperationType.Modify;
                case "create": return ChangeStreamOperationType.Create;
                case "shardCollection": return ChangeStreamOperationType.ShardCollection;
                case "refineCollectionShardKey": return ChangeStreamOperationType.RefineCollectionShardKey;
                case "reshardCollection": return ChangeStreamOperationType.ReshardCollection;
                default: return (ChangeStreamOperationType)(-1);
            }
        }

        /// <inheritdoc />
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ChangeStreamOperationType value)
        {
            var writer = context.Writer;

            switch (value)
            {
                case ChangeStreamOperationType.Delete: writer.WriteString("delete"); break;
                case ChangeStreamOperationType.Insert: writer.WriteString("insert"); break;
                case ChangeStreamOperationType.Invalidate: writer.WriteString("invalidate"); break;
                case ChangeStreamOperationType.Replace: writer.WriteString("replace"); break;
                case ChangeStreamOperationType.Update: writer.WriteString("update"); break;
                case ChangeStreamOperationType.Rename: writer.WriteString("rename"); break;
                case ChangeStreamOperationType.Drop: writer.WriteString("drop"); break;
                case ChangeStreamOperationType.DropDatabase: writer.WriteString("dropDatabase"); break;
                case ChangeStreamOperationType.CreateIndexes: writer.WriteString("createIndexes"); break;
                case ChangeStreamOperationType.DropIndexes: writer.WriteString("dropIndexes"); break;
                case ChangeStreamOperationType.Modify: writer.WriteString("modify"); break;
                case ChangeStreamOperationType.Create: writer.WriteString("create"); break;
                case ChangeStreamOperationType.ShardCollection: writer.WriteString("shardCollection"); break;
                case ChangeStreamOperationType.RefineCollectionShardKey: writer.WriteString("refineCollectionShardKey"); break;
                case ChangeStreamOperationType.ReshardCollection: writer.WriteString("reshardCollection"); break;
                default: throw new ArgumentException($"Invalid ChangeStreamOperationType: {value}.", nameof(value));
            }
        }
    }
}
