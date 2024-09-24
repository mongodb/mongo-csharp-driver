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

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    internal sealed class Type1SectionFormatter : ICommandMessageSectionFormatter<Type1CommandMessageSection>
    {
        private readonly long? _maxSize;

        public Type1SectionFormatter(long? maxSize)
        {
            _maxSize = maxSize;
        }

        public void FormatSection(Type1CommandMessageSection section, IBsonWriter writer)
        {
            if (writer is not BsonBinaryWriter binaryWriter)
            {
                throw new ArgumentException("Writer must be an instance of BsonBinaryWriter");
            }

            var stream = binaryWriter.BsonStream;
            var serializer = section.DocumentSerializer;
            var context = BsonSerializationContext.CreateRoot(binaryWriter);
            var startPosition = stream.Position;

            stream.WriteInt32(0); // size
            stream.WriteCString(section.Identifier);

            var batch = section.Documents;
            var maxDocumentSize = section.MaxDocumentSize ?? binaryWriter.Settings.MaxDocumentSize;
            binaryWriter.PushSettings(s => ((BsonBinaryWriterSettings)s).MaxDocumentSize = maxDocumentSize);
            binaryWriter.PushElementNameValidator(section.ElementNameValidator);
            try
            {
                var maxBatchCount = Math.Min(batch.Count, section.MaxBatchCount ?? int.MaxValue);
                var processedCount = maxBatchCount;
                for (var i = 0; i < maxBatchCount; i++)
                {
                    var documentStartPosition = stream.Position;
                    var document = batch.Items[batch.Offset + i];
                    serializer.Serialize(context, document);

                    var writtenSize = stream.Position - startPosition;
                    if (writtenSize > _maxSize && batch.CanBeSplit && i > 0)
                    {
                        stream.Position = documentStartPosition;
                        stream.SetLength(documentStartPosition);
                        processedCount = i;
                        break;
                    }
                }
                batch.SetProcessedCount(processedCount);
            }
            finally
            {
                writer.PopElementNameValidator();
                writer.PopSettings();
            }
            stream.BackpatchSize(startPosition);
        }
    }
}
