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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver
{
    internal class ChangeStreamDocumentDatabaseNamespaceSerializer : SealedClassSerializerBase<DatabaseNamespace>, IBsonDocumentSerializer
    {
        #region static
        // private static fields
        private static readonly ChangeStreamDocumentDatabaseNamespaceSerializer __instance = new ChangeStreamDocumentDatabaseNamespaceSerializer();

        // public static properties
        public static ChangeStreamDocumentDatabaseNamespaceSerializer Instance => __instance;
        #endregion

        // public methods
        public override DatabaseNamespace Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
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

                    default:
                        reader.SkipValue();
                        break;
                }
            }
            reader.ReadEndDocument();

            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                return new DatabaseNamespace(databaseName);
            }

            return null;
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            if (memberName == "DatabaseName")
            {
                serializationInfo = new BsonSerializationInfo("db", StringSerializer.Instance, typeof(string));
                return true;
            }

            serializationInfo = null;
            return false;
        }

        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, DatabaseNamespace value)
        {
            var writer = context.Writer;

            writer.WriteStartDocument();
            writer.WriteName("db");
            writer.WriteString(value.DatabaseName);
            writer.WriteEndDocument();
        }
    }
}
