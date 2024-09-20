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

using System.Collections.Generic;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    internal sealed class ClientBulkWriteOpsSectionFormatter : ICommandMessageSectionFormatter<ClientBulkWriteOpsCommandMessageSection>
    {
        public void FormatSection(ClientBulkWriteOpsCommandMessageSection section, BsonBinaryWriter writer, long? maxSize)
        {
            throw new System.NotImplementedException();
        }

        private void EncodeInsertOneOperation(BsonSerializationContext context, BulkInsertOneOperation op, int nsInfoIndex)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            writer.WriteName("insert");
            writer.WriteInt32(nsInfoIndex);
            writer.WriteName("document");
            BsonDocumentSerializer.Instance.Serialize(context, op);
            writer.WriteEndDocument();
        }

    }
}
