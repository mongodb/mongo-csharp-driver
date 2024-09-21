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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class CreateIndexRequest
    {
        // constructors
        public CreateIndexRequest(BsonDocument keys)
        {
            Keys = Ensure.IsNotNull(keys, nameof(keys));
        }

        // properties
        public BsonDocument AdditionalOptions { get; set; }
        public bool? Background { get; set; }
        public int? Bits { get; set; }
        [Obsolete("GeoHaystack indexes were deprecated in server version 4.4.")]
        public double? BucketSize { get; set; }
        public Collation Collation { get; set; }
        public string DefaultLanguage { get; set; }
        public TimeSpan? ExpireAfter { get; set; }
        public bool? Hidden { get; set; }
        public string LanguageOverride { get; set; }
        public BsonDocument Keys { get; }
        public double? Max { get; set; }
        public double? Min { get; set; }
        public string Name { get; set; }
        public BsonDocument PartialFilterExpression { get; set; }
        public bool? Sparse { get; set; }
        public int? SphereIndexVersion { get; set; }
        public BsonDocument StorageEngine { get; set; }
        public int? TextIndexVersion { get; set; }
        public bool? Unique { get; set; }
        public int? Version { get; set; }
        public BsonDocument Weights { get; set; }
        public BsonDocument WildcardProjection { get; set; }

        // public methods
        public string GetIndexName()
        {
            if (Name != null)
            {
                return Name;
            }

            if (AdditionalOptions != null)
            {
                BsonValue name;
                if (AdditionalOptions.TryGetValue("name", out name))
                {
                    return name.AsString;
                }
            }

            return IndexNameHelper.GetIndexName(Keys);
        }

        // methods
        public BsonDocument CreateIndexDocument()
        {
            var document = new BsonDocument
            {
                { "key", Keys },
                { "name", GetIndexName() },
                { "background", () => Background.Value, Background.HasValue },
                { "bits", () => Bits.Value, Bits.HasValue },
#pragma warning disable CS0618 // Type or member is obsolete
                { "bucketSize", () => BucketSize.Value, BucketSize.HasValue },
#pragma warning restore CS0618 // Type or member is obsolete
                { "collation", () => Collation.ToBsonDocument(), Collation != null },
                { "default_language", () => DefaultLanguage, DefaultLanguage != null },
                { "expireAfterSeconds", () => ExpireAfter.Value.TotalSeconds, ExpireAfter.HasValue },
                { "hidden", () => Hidden.Value, Hidden.HasValue },
                { "language_override", () => LanguageOverride, LanguageOverride != null },
                { "max", () => Max.Value, Max.HasValue },
                { "min", () => Min.Value, Min.HasValue },
                { "partialFilterExpression", PartialFilterExpression, PartialFilterExpression != null },
                { "sparse", () => Sparse.Value, Sparse.HasValue },
                { "2dsphereIndexVersion", () => SphereIndexVersion.Value, SphereIndexVersion.HasValue },
                { "storageEngine", () => StorageEngine, StorageEngine != null },
                { "textIndexVersion", () => TextIndexVersion.Value, TextIndexVersion.HasValue },
                { "unique", () => Unique.Value, Unique.HasValue },
                { "v", () => Version.Value, Version.HasValue },
                { "weights", () => Weights, Weights != null },
                { "wildcardProjection", WildcardProjection, WildcardProjection != null }
            };

            if (AdditionalOptions != null)
            {
                document.Merge(AdditionalOptions, overwriteExistingElements: false);
            }
            return document;
        }
    }
}
