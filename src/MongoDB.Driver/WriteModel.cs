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
    public abstract class WriteModel<T>
    {
        // static methods
        // These static methods are only called from the Legacy
        // API, so there is type safety in how they got allowed
        // into the system, meaning that even though
        // some things below seem unsafe, they are in a roundabout
        // way.
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

        private static WriteModel<T> ConvertDeleteRequest(DeleteRequest deleteRequest)
        {
            if(deleteRequest.Limit == 1)
            {
                return new RemoveOneModel<T>(deleteRequest.Criteria);
            }

            return new RemoveManyModel<T>(deleteRequest.Criteria);
        }

        private static WriteModel<T> ConvertInsertRequest(InsertRequest request)
        {
            object document;
            var wrapper = request.Document as BsonDocumentWrapper;
            if (wrapper != null)
            {
                document = wrapper.Wrapped;
            }
            else
            {
                document = request.Document;
            }

            return new InsertOneModel<T>((T)document);
        }

        private static WriteModel<T> ConvertUpdateRequest(UpdateRequest updateRequest)
        {
            if(updateRequest.IsMulti)
            {
                return new UpdateManyModel<T>(updateRequest.Criteria, updateRequest.Update)
                {
                    IsUpsert = updateRequest.IsUpsert
                };
            }

            var firstElement = updateRequest.Update.GetElement(0).Name;
            if(firstElement.StartsWith("$"))
            {
                return new UpdateOneModel<T>(updateRequest.Criteria, updateRequest.Update)
                {
                    IsUpsert = updateRequest.IsUpsert
                };
            }

            return ConvertToReplaceOne(updateRequest);
        }

        private static WriteModel<T> ConvertToReplaceOne(UpdateRequest updateRequest)
        {
            object document;
            var wrappedUpdate = updateRequest.Update as BsonDocumentWrapper;
            if (wrappedUpdate != null)
            {
                document = wrappedUpdate.Wrapped;
            }
            else
            {
                document = updateRequest.Update;
            }

            return new ReplaceOneModel<T>(updateRequest.Criteria, (T)document)
            {
                IsUpsert = updateRequest.IsUpsert
            };
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
