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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents base class for all operations in the scope of bulk write.
    /// </summary>
    public abstract class BulkWriteModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteModel"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        protected BulkWriteModel(CollectionNamespace collectionNamespace)
        {
            Namespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
        }

        /// <summary>
        /// The namespace on which to perform the operation.
        /// </summary>
        public CollectionNamespace Namespace { get; }

        internal abstract bool IsMulti { get; }

        internal abstract void Render(RenderArgs<BsonDocument> renderArgs, BsonSerializationContext serializationContext, IBulkWriteModelRenderer renderer);
    }
}
