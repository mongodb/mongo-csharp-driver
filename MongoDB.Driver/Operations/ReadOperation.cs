/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Operations
{
    internal abstract class ReadOperation : DatabaseOperation
    {
        protected ReadOperation(
            string databaseName,
            string collectionName,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings)
            : base(databaseName, collectionName, readerSettings, writerSettings)
        {
        }

        protected IMongoQuery WrapQuery(IMongoQuery query, BsonDocument options, ReadPreference readPreference, bool forShardRouter)
        {
            BsonDocument formattedReadPreference = null;
            if (forShardRouter && readPreference != null && readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary)
            {
                BsonArray tagSetsArray = null;
                if (readPreference.TagSets != null)
                {
                    tagSetsArray = new BsonArray();
                    foreach (var tagSet in readPreference.TagSets)
                    {
                        var tagSetDocument = new BsonDocument();
                        foreach (var tag in tagSet)
                        {
                            tagSetDocument.Add(tag.Name, tag.Value);
                        }
                        tagSetsArray.Add(tagSetDocument);
                    }
                }

                if (tagSetsArray != null || readPreference.ReadPreferenceMode != ReadPreferenceMode.SecondaryPreferred)
                {
                    formattedReadPreference = new BsonDocument
                    {
                        { "mode", MongoUtils.ToCamelCase(readPreference.ReadPreferenceMode.ToString()) },
                        { "tags", tagSetsArray, tagSetsArray != null } // optional
                    };
                }
            }

            if (options == null && formattedReadPreference == null)
            {
                return query;
            }
            else
            {
                var queryDocument = (query == null) ? (BsonValue)new BsonDocument() : BsonDocumentWrapper.Create(query);
                var wrappedQuery = new QueryDocument
                {
                    { "$query", queryDocument },
                    { "$readPreference", formattedReadPreference, formattedReadPreference != null }, // only if sending query to a mongos
                };
                wrappedQuery.Merge(options);
                return wrappedQuery;
            }
        }
    }
}
