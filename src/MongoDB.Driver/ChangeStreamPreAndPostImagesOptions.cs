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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver
{
    /// <summary>
    /// Change stream pre and post images options.
    /// </summary>
    public sealed class ChangeStreamPreAndPostImagesOptions : BsonDocumentBackedClass
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeStreamPreAndPostImagesOptions"/> class.
        /// </summary>
        public ChangeStreamPreAndPostImagesOptions()
            : base(ChangeStreamPreAndPostImagesOptionsSerializer.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeStreamPreAndPostImagesOptions"/> class.
        /// </summary>
        /// <param name="bsonDocument">The backing document.</param>
        public ChangeStreamPreAndPostImagesOptions(BsonDocument bsonDocument)
            : base(bsonDocument, ChangeStreamPreAndPostImagesOptionsSerializer.Instance)
        {
        }

        // public properties
        /// <summary>
        /// Gets the backing document.
        /// </summary>
        new public BsonDocument BackingDocument => base.BackingDocument;

        /// <summary>
        /// Gets or sets a value indicating whether ChangeStreamPreAndPostImages is enabled.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return GetValue(nameof(Enabled), false);
            }
            set
            {
                SetValue(nameof(Enabled), value);
            }
        }
    }

    internal sealed class ChangeStreamPreAndPostImagesOptionsSerializer : BsonDocumentBackedClassSerializer<ChangeStreamPreAndPostImagesOptions>
    {
        public static ChangeStreamPreAndPostImagesOptionsSerializer Instance { get; } = new ChangeStreamPreAndPostImagesOptionsSerializer();

        // constructors
        public ChangeStreamPreAndPostImagesOptionsSerializer()
        {
            RegisterMember("Enabled", "enabled", new BooleanSerializer());
        }

        // protected methods
        protected override ChangeStreamPreAndPostImagesOptions CreateInstance(BsonDocument backingDocument) =>
            new ChangeStreamPreAndPostImagesOptions(backingDocument);
    }
}
