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

using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    internal static class BsonSerializerExtensions
    {
        public static object SetDocumentIdIfMissing<TDocument>(this IBsonSerializer<TDocument> serializer, object container, TDocument document)
        {
            var idProvider = serializer as IBsonIdProvider;
            if (idProvider != null)
            {
                object id;
                IIdGenerator idGenerator;
                if (idProvider.GetDocumentId(document, out id, out _, out idGenerator))
                {
                    if (idGenerator != null && idGenerator.IsEmpty(id))
                    {
                        id = idGenerator.GenerateId(container, document);
                        idProvider.SetDocumentId(document, id);
                    }
                }

                return id;
            }

            return null;
        }
    }
}
