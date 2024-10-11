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

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    internal sealed class Type1SectionFormatter : ICommandMessageSectionFormatter<Type1CommandMessageSection>
    {
        public void FormatSection(Type1CommandMessageSection section, IBsonWriter writer)
        {
            writer.WriteString("identifier", section.Identifier);
            writer.WriteName("documents");
            writer.WriteStartArray();
            var batch = section.Documents;
            var serializer = section.DocumentSerializer;
            var context = BsonSerializationContext.CreateRoot(writer);
            for (var i = 0; i < batch.Count; i++)
            {
                var document = batch.Items[batch.Offset + i];
                serializer.Serialize(context, document);
            }
            writer.WriteEndArray();
        }
    }
}
