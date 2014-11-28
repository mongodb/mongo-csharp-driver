/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    /// <summary>
    /// Base class for a write model.
    /// </summary>
    [Serializable]
    public abstract class WriteModel<T>
    {
        // static methods
        // These static methods are only called from the Legacy
        // API, so there is type safety in how they got allowed
        // into the system, meaning that even though
        // some things below seem unsafe, they are in a roundabout
        // way. In addition, we know that there will always 
        // be one level of BsonDocumentWrapper for everything, even
        // when the type is already a BsonDocument :(.
        internal static WriteModel<T> FromCore(WriteRequest request)
        {
            switch (request.RequestType)
            {
                case WriteRequestType.Delete:
                    return ConvertDeleteRequest((DeleteRequest)request);
                case WriteRequestType.Insert:
                    return ConvertInsertRequest((InsertRequest)request);
                case WriteRequestType.Update:
                    return ConvertUpdateRequest((UpdateRequest)request);
                default:
                    throw new NotSupportedException();
            }
        }

        private static WriteModel<T> ConvertDeleteRequest(DeleteRequest request)
        {
            var filter = Unwrap(request.Filter);
            if(request.Limit == 1)
            {
                return new DeleteOneModel<T>(filter);
            }

            return new DeleteManyModel<T>(filter);
        }

        private static WriteModel<T> ConvertInsertRequest(InsertRequest request)
        {
            var document = (T)Unwrap(request.Document);
            return new InsertOneModel<T>(document);
        }

        private static WriteModel<T> ConvertUpdateRequest(UpdateRequest request)
        {
            var filter = Unwrap(request.Filter);
            var update = Unwrap(request.Update);
            if(request.IsMulti)
            {
                return new UpdateManyModel<T>(filter, update)
                {
                    IsUpsert = request.IsUpsert
                };
            }

            var firstElement = request.Update.GetElement(0).Name;
            if(firstElement.StartsWith("$"))
            {
                return new UpdateOneModel<T>(filter, update)
                {
                    IsUpsert = request.IsUpsert
                };
            }

            return ConvertToReplaceOne(request);
        }

        private static WriteModel<T> ConvertToReplaceOne(UpdateRequest request)
        {
            var document = (T)Unwrap(request.Update);

            return new ReplaceOneModel<T>(Unwrap(request.Filter), document)
            {
                IsUpsert = request.IsUpsert
            };
        }

        private static object Unwrap(BsonDocument wrapper)
        {
            return ((BsonDocumentWrapper)wrapper).Wrapped;
        }

        // constructors
        // only MongoDB can define new write models.
        internal WriteModel()
        {
        }

        // properties
        /// <summary>
        /// Gets the type of the model.
        /// </summary>
        public abstract WriteModelType ModelType { get; }
    }
}
