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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents base class for all operations in scope of bulk write.
    /// </summary>
    public abstract class BulkWriteModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteModel"/> class.
        /// </summary>
        protected BulkWriteModel()
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteModel"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        protected BulkWriteModel(CollectionNamespace collectionNamespace)
        {
            Namespace = collectionNamespace;
        }

        /// <summary>
        /// The namespace on which to perform the operation.
        /// </summary>
        public CollectionNamespace Namespace { get; init; }

        internal abstract int WriteTo(BulkWriteModelSerializationContext context);
    }
}
