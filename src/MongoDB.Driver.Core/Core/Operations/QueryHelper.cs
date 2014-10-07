/* Copyright 2013-2014 MongoDB Inc.
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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Operations
{
    internal static class QueryHelper
    {
        public static int CalculateFirstBatchSize(int? limit, int? batchSize)
        {
            int firstBatchSize;

            int nonNullLimit = limit ?? 0;
            int nonNullBatchSize = batchSize ?? 0;
            if (nonNullLimit < 0)
            {
                firstBatchSize = nonNullLimit;
            }
            else if (nonNullLimit == 0)
            {
                firstBatchSize = nonNullBatchSize;
            }
            else if (nonNullBatchSize == 0)
            {
                firstBatchSize = nonNullLimit;
            }
            else if (nonNullLimit < nonNullBatchSize)
            {
                firstBatchSize = nonNullLimit;
            }
            else
            {
                firstBatchSize = nonNullBatchSize;
            }

            return firstBatchSize;
        }

        public static BsonDocument CreateReadPreferenceDocument(ServerType serverType, ReadPreference readPreference)
        {
            if (readPreference == null)
            {
                return null;
            }
            if (serverType != ServerType.ShardRouter)
            {
                return null;
            }

            BsonArray tagSets = null;
            if (readPreference.TagSets != null && readPreference.TagSets.Any())
            {
                tagSets = new BsonArray(readPreference.TagSets.Select(ts => new BsonDocument(ts.Tags.Select(t => new BsonElement(t.Name, t.Value)))));
            }
            else if (readPreference.ReadPreferenceMode == ReadPreferenceMode.Primary || readPreference.ReadPreferenceMode == ReadPreferenceMode.SecondaryPreferred)
            {
                return null;
            }

            var readPreferenceString = readPreference.ReadPreferenceMode.ToString();
            readPreferenceString = Char.ToLowerInvariant(readPreferenceString[0]) + readPreferenceString.Substring(1);

            return new BsonDocument
            {
                { "mode", readPreferenceString },
                { "tags", tagSets, tagSets != null }
            };
        }
    }
}
