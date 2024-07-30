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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver
{
    internal class ChangeStreamSplitEventSerializer : SealedClassSerializerBase<ChangeStreamSplitEvent>
    {
        #region static

        public static ChangeStreamSplitEventSerializer Instance { get; } = new ChangeStreamSplitEventSerializer();

        #endregion

        protected override ChangeStreamSplitEvent DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            var fragment = -1;
            var of = -1;

            reader.ReadStartDocument();
            while (reader.ReadBsonType() != 0)
            {
                var fieldName = reader.ReadName();
                switch (fieldName)
                {
                    case "fragment":
                        fragment = reader.ReadInt32();
                        break;

                    case "of":
                        of = reader.ReadInt32();
                        break;
                    default:
                        throw new FormatException($"Invalid field name: \"{fieldName}\".");
                }
            }
            reader.ReadEndDocument();

            return new(fragment, of);
        }

        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, ChangeStreamSplitEvent value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            writer.WriteName("fragment");
            writer.WriteInt32(value.Fragment);
            writer.WriteName("of");
            writer.WriteInt32(value.Of);
            writer.WriteEndDocument();
        }
    }
}
