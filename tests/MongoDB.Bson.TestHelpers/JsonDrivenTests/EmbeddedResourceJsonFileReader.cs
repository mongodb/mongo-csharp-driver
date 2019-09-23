/* Copyright 2019-present MongoDB Inc.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.TestHelpers.JsonDrivenTests
{
    public abstract class EmbeddedResourceJsonFileReader
    {
        // protected properties
        protected virtual Assembly Assembly => this.GetType().GetTypeInfo().Assembly;

        protected virtual string PathPrefix { get; } = null;
        protected virtual string[] PathPrefixes { get; } = null;

        protected virtual BsonDocument ReadJsonDocument(string path)
        {
            var jsonReaderSettings = new JsonReaderSettings { GuidRepresentation = GuidRepresentation.Unspecified };
            using (var stream = Assembly.GetManifestResourceStream(path))
            using (var streamReader = new StreamReader(stream))
            using (var jsonReader = new JsonReader(streamReader, jsonReaderSettings))
            {
                var context = BsonDeserializationContext.CreateRoot(jsonReader);
                var document = BsonDocumentSerializer.Instance.Deserialize(context);
                document.InsertAt(0, new BsonElement("_path", path));
                return document;
            }
        }

        protected virtual IEnumerable<BsonDocument> ReadJsonDocuments()
        {
            return
                Assembly.GetManifestResourceNames()
                    .Where(path => ShouldReadJsonDocument(path))
                    .Select(path => ReadJsonDocument(path));
        }

        protected virtual bool ShouldReadJsonDocument(string path)
        {
            var prefixes = GetPathPrefixes();
            return prefixes.Any(path.StartsWith) && path.EndsWith(".json");
        }

        private string[] GetPathPrefixes()
        {
            var prefixes = !string.IsNullOrEmpty(PathPrefix) ? new[] { PathPrefix } : PathPrefixes;

            if (prefixes == null || prefixes.Length == 0)
            {
                throw new NotImplementedException("At least one path prefix must be specified.");
            }

            return prefixes;
        }
    }
}
