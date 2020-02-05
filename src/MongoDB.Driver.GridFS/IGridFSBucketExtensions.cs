/* Copyright 2016-present MongoDB Inc.
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
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.GridFS
{
    internal static class IGridFSBucketExtensions
    {
        internal static CollectionNamespace GetChunksCollectionNamespace<TFileId>(this IGridFSBucket<TFileId> bucket)
        {
            var databaseNamespace = bucket.Database.DatabaseNamespace;
            var collectionName = bucket.Options.BucketName + ".chunks";
            return new CollectionNamespace(databaseNamespace, collectionName);
        }

        internal static CollectionNamespace GetFilesCollectionNamespace<TFileId>(this IGridFSBucket<TFileId> bucket)
        {
            var databaseNamespace = bucket.Database.DatabaseNamespace;
            var collectionName = bucket.Options.BucketName + ".files";
            return new CollectionNamespace(databaseNamespace, collectionName);
        }

        internal static MessageEncoderSettings GetMessageEncoderSettings<TFileId>(this IGridFSBucket<TFileId> bucket)
        {
            var databaseSettings = bucket.Database.Settings;
            var messageEncoderSettings = new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.ReadEncoding,  databaseSettings.ReadEncoding ?? Utf8Encodings.Strict },
                { MessageEncoderSettingsName.WriteEncoding,  databaseSettings.WriteEncoding ?? Utf8Encodings.Strict }
            };
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                messageEncoderSettings.Add(MessageEncoderSettingsName.GuidRepresentation, databaseSettings.GuidRepresentation);
            }
#pragma warning restore 618
            return messageEncoderSettings;
        }
    }
}
