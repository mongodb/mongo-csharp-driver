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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver
{
    internal class ChangeStreamDocumentCollectionNamespaceSerializer : SealedClassSerializerBase<CollectionNamespace>, IBsonDocumentSerializer
    {
        #region static
        // private static fields
        private static readonly ChangeStreamDocumentCollectionNamespaceSerializer __instance = new ChangeStreamDocumentCollectionNamespaceSerializer();

        // public static properties
        public static ChangeStreamDocumentCollectionNamespaceSerializer Instance => __instance;
        #endregion

        // public methods
        public override CollectionNamespace Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            string collectionName = null;
            string databaseName = null;

            reader.ReadStartDocument();

            while (reader.ReadBsonType() != 0)
            {
                var fieldName = reader.ReadName();

                switch (fieldName)
                {
                    case "db" when reader.CurrentBsonType == BsonType.String:
                        databaseName = reader.ReadString();
                        break;

                    case "coll" when reader.CurrentBsonType == BsonType.String:
                        collectionName = reader.ReadString();
                        break;

                    default:
                        reader.SkipValue();
                        break;
                }
            }
            reader.ReadEndDocument();

            if (!string.IsNullOrWhiteSpace(databaseName) && !string.IsNullOrWhiteSpace(collectionName))
            {
                var databaseNamespace = new DatabaseNamespace(databaseName);
                return new CollectionNamespace(databaseNamespace, collectionName);
            }

            return null;
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            if (memberName == "CollectionName")
            {
                serializationInfo = new BsonSerializationInfo("coll", StringSerializer.Instance, typeof(string));
                return true;
            }

            serializationInfo = null;
            return false;
        }

        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, CollectionNamespace value)
        {
            var writer = context.Writer;

            writer.WriteStartDocument();
            writer.WriteName("db");
            writer.WriteString(value.DatabaseNamespace.DatabaseName);
            writer.WriteName("coll");
            writer.WriteString(value.CollectionName);
            writer.WriteEndDocument();
        }
    }
}
