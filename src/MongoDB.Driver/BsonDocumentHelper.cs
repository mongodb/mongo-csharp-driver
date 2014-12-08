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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver
{
    internal static class BsonDocumentHelper
    {
        public static BsonDocument ToBsonDocument(IBsonSerializerRegistry registry, object document)
        {
            if (document == null)
            {
                return null;
            }

            var bsonDocument = document as BsonDocument;
            if (bsonDocument != null)
            {
                return bsonDocument;
            }

            if (document is string)
            {
                return BsonDocument.Parse((string)document);
            }

            var serializer = registry.GetSerializer(document.GetType());
            return new BsonDocumentWrapper(document, serializer);
        }

        public static BsonDocument FilterToBsonDocument<TDocument>(IBsonSerializerRegistry registry, object filter)
        {
            var filterExpr = filter as Expression<Func<TDocument, bool>>;
            if (filterExpr != null)
            {
                var helper = new BsonSerializationInfoHelper();
                helper.RegisterExpressionSerializer(filterExpr.Parameters[0], registry.GetSerializer<TDocument>());
                return new QueryBuilder<TDocument>(helper).Where(filterExpr).ToBsonDocument();
            }

            return ToBsonDocument(registry, filter);
        }
    }
}
