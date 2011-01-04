/* Copyright 2010 10gen Inc.
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
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Builders {
    public static class Query {
        #region public static properties
        public static IMongoQuery Null {
            get { return null; }
        }
        #endregion

        #region public static methods
        public static QueryConditionList All(
            string name,
            BsonArray array
        ) {
            return new QueryConditionList(name, "$all", array);
        }

        public static QueryConditionList All(
            string name,
            params BsonValue[] values
        ) {
            return new QueryConditionList(name, "$all", new BsonArray((IEnumerable<BsonValue>) values));
        }

        public static QueryComplete And(
            params QueryComplete[] queries
        ) {
            var document = new BsonDocument();
            foreach (var query in queries) {
                var queryDocument = query.ToBsonDocument();
                foreach (BsonElement queryElement in queryDocument) {

                    // Automatically support {"name": {$op1: ..., $op2: ...}} from a single Query.And(
                    if (document.Contains(queryElement.Name)) {
                        var subDocumentValue = document[queryElement.Name];
                        var subQueriesValue = queryElement.Value;

                        // Make sure that no conditions are Query.EQ, because duplicates aren't allowed
                        if (
                            subDocumentValue.BsonType != BsonType.Document ||
                            subQueriesValue.BsonType != BsonType.Document) {

                                throw new InvalidOperationException(
                                    string.Format(
                                    "Can not use Query.And with multiple Query.EQ for the same value: {0}",
                                    queryElement.Name));
                        }

                        var subDocument = subDocumentValue.AsBsonDocument;
                        var subQueries = subQueriesValue.AsBsonDocument;

                        // Add each subquery
                        foreach (BsonElement subQueryElement in subQueries) {
                            // Make sure that there are no duplicate $operators
                            if (subDocument.Contains(subQueryElement.Name)) {
                                throw new InvalidOperationException(
                                    string.Format(
                                    "Can not use Query.And with multiple {1} for the same value: {0}",
                                    queryElement.Name,
                                    subQueryElement.Name));
                            } else {
                                subDocument[subQueryElement.Name] = subQueryElement.Value;
                            }
                        }
                    } else {
                        document[queryElement.Name] = queryElement.Value;
                    }
                }
            }
            return new QueryComplete(document);
        }

        public static QueryConditionList ElemMatch(
            string name,
            QueryComplete query
        ) {
            return new QueryConditionList(name, "$elemMatch", query.ToBsonDocument());
        }

        public static QueryComplete EQ(
            string name,
            BsonValue value
        ) {
            return new QueryComplete(new BsonDocument(name, value));
        }

        public static QueryConditionList Exists(
            string name,
            bool value
        ) {
            return new QueryConditionList(name, "$exists", BsonBoolean.Create(value));
        }

        public static QueryConditionList GT(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name, "$gt", value);
        }

        public static QueryConditionList GTE(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name, "$gte", value);
        }

        public static QueryConditionList In(
            string name,
            BsonArray value
        ) {
            return new QueryConditionList(name, "$in", value);
        }

        public static QueryConditionList In(
            string name,
            params BsonValue[] values
        ) {
            return new QueryConditionList(name, "$in", new BsonArray((IEnumerable<BsonValue>) values));
        }

        public static QueryConditionList LT(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name, "$lt", value);
        }

        public static QueryConditionList LTE(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name, "$lte", value);
        }

        public static QueryComplete Matches(
            string name,
            BsonRegularExpression regex
        ) {
            return new QueryComplete(new BsonDocument(name, regex));
        }

        public static QueryConditionList Mod(
            string name,
            int modulus,
            int equals
        ) {
            return new QueryConditionList(name, "$mod", new BsonArray { modulus, equals });
        }

        public static QueryConditionList NE(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name, "$ne", value);
        }

        public static QueryConditionList NotIn(
            string name,
            BsonArray array
        ) {
            return new QueryConditionList(name, "$nin", array);
        }

        public static QueryConditionList NotIn(
            string name,
            params BsonValue[] values
        ) {
            return new QueryConditionList(name, "$nin", new BsonArray((IEnumerable<BsonValue>) values));
        }

        public static QueryNot Not(
            string name
        ) {
            return new QueryNot(name);
        }

        public static QueryComplete Or(
            params QueryComplete[] queries
        ) {
            var array = new BsonArray();
            foreach (var query in queries) {
                array.Add(query.ToBsonDocument());
            }
            var document = new BsonDocument("$or", array);
            return new QueryComplete(document);
        }

        public static QueryConditionList Size(
            string name,
            int size
        ) {
            return new QueryConditionList(name, "$size", size);
        }

        public static QueryConditionList Type(
            string name,
            BsonType type
        ) {
            return new QueryConditionList(name, "$type", (int) type);
        }

        public static QueryComplete Where(
            BsonJavaScript javaScript
        ) {
            return new QueryComplete(new BsonDocument("$where", javaScript));
        }

        public static IMongoQuery Wrap(
            object query
        ) {
            return QueryWrapper.Create(query);
        }
        #endregion
    }

    [Serializable]
    public abstract class QueryBuilder : BuilderBase {
        #region private fields
        protected BsonDocument document;
        #endregion

        #region constructors
        protected QueryBuilder(
            BsonDocument document
        ) {
            this.document = document;
        }
        #endregion

        #region public methods
        public override BsonDocument ToBsonDocument() {
            return document;
        }
        #endregion

        #region protected methods
        protected override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            document.Serialize(bsonWriter, nominalType, options);
        }
        #endregion
    }

    [Serializable]
    public class QueryComplete : QueryBuilder, IMongoQuery {
        #region constructors
        public QueryComplete(
            BsonDocument document
        )
            : base(document) {
        }
        #endregion
    }

    [Serializable]
    public class QueryConditionList : QueryComplete {
        #region private fields
        private BsonDocument conditions;
        #endregion

        #region constructors
        public QueryConditionList(
            string name,
            string op,
            BsonValue value
        )
            : base(new BsonDocument(name, new BsonDocument(op, value))) {
            conditions = document[0].AsBsonDocument;
        }
        #endregion

        #region public methods
        public QueryConditionList All(
            BsonArray array
        ) {
            conditions.Add("$all", array);
            return this;
        }

        public QueryConditionList All(
            params BsonValue[] values
        ) {
            conditions.Add("$all", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        public QueryConditionList ElemMatch(
            QueryComplete query
        ) {
            conditions.Add("$elemMatch", query.ToBsonDocument());
            return this;
        }

        public QueryConditionList Exists(
            bool value
        ) {
            conditions.Add("$exists", BsonBoolean.Create(value));
            return this;
        }

        public QueryConditionList GT(
            BsonValue value
        ) {
            conditions.Add("$gt", value);
            return this;
        }

        public QueryConditionList GTE(
            BsonValue value
        ) {
            conditions.Add("$gte", value);
            return this;
        }

        public QueryConditionList In(
            BsonArray array
        ) {
            conditions.Add("$in", array);
            return this;
        }

        public QueryConditionList In(
            params BsonValue[] values
        ) {
            conditions.Add("$in", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        public QueryConditionList LT(
            BsonValue value
        ) {
            conditions.Add("$lt", value);
            return this;
        }

        public QueryConditionList LTE(
            BsonValue value
        ) {
            conditions.Add("$lte", value);
            return this;
        }

        public QueryConditionList Mod(
            int modulus,
            int equals
        ) {
            conditions.Add("$mod", new BsonArray { modulus, equals });
            return this;
        }

        public QueryConditionList NE(
            BsonValue value
        ) {
            conditions.Add("$ne", value);
            return this;
        }

        public QueryConditionList NotIn(
            BsonArray array
        ) {
            conditions.Add("$nin", array);
            return this;
        }

        public QueryConditionList NotIn(
            params BsonValue[] values
        ) {
            conditions.Add("$nin", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        public QueryConditionList Size(
            int size
        ) {
            conditions.Add("$size", size);
            return this;
        }

        public QueryConditionList Type(
            BsonType type
        ) {
            conditions.Add("$type", (int) type);
            return this;
        }
        #endregion
    }

    public class QueryNot {
        #region private fields
        private string name;
        #endregion

        #region constructors
        public QueryNot(
            string name
        ) {
            this.name = name;
        }
        #endregion

        #region public methods
        public QueryNotConditionList All(
            BsonArray array
        ) {
            return new QueryNotConditionList(name, "$all", array);
        }

        public QueryNotConditionList All(
            params BsonValue[] values
        ) {
            return new QueryNotConditionList(name, "$all", new BsonArray((IEnumerable<BsonValue>) values));
        }

        public QueryNotConditionList ElemMatch(
            QueryComplete query
        ) {
            return new QueryNotConditionList(name, "$elemMatch", query.ToBsonDocument());
        }

        public QueryNotConditionList Exists(
            bool value
        ) {
            return new QueryNotConditionList(name, "$exists", BsonBoolean.Create(value));
        }

        public QueryNotConditionList GT(
            BsonValue value
        ) {
            return new QueryNotConditionList(name, "$gt", value);
        }

        public QueryNotConditionList GTE(
            BsonValue value
        ) {
            return new QueryNotConditionList(name, "$gte", value);
        }

        public QueryNotConditionList In(
            BsonArray array
        ) {
            return new QueryNotConditionList(name, "$in", array);
        }

        public QueryNotConditionList In(
            params BsonValue[] values
        ) {
            return new QueryNotConditionList(name, "$in", new BsonArray((IEnumerable<BsonValue>) values));
        }

        public QueryNotConditionList Mod(
            int modulus,
            int equals
        ) {
            return new QueryNotConditionList(name, "$mod", new BsonArray { modulus, equals });
        }

        public QueryNotConditionList NE(
            BsonValue value
        ) {
            return new QueryNotConditionList(name, "$ne", value);
        }

        public QueryNotConditionList NotIn(
            BsonArray array
        ) {
            return new QueryNotConditionList(name, "nin", array);
        }

        public QueryNotConditionList NotIn(
            params BsonValue[] values
        ) {
            return new QueryNotConditionList(name, "nin", new BsonArray((IEnumerable<BsonValue>) values));
        }

        public QueryNotConditionList LT(
            BsonValue value
        ) {
            return new QueryNotConditionList(name, "$lt", value);
        }

        public QueryNotConditionList LTE(
            BsonValue value
        ) {
            return new QueryNotConditionList(name, "$lte", value);
        }

        public QueryComplete Matches(
            BsonRegularExpression regex
        ) {
            return new QueryComplete(new BsonDocument(name, new BsonDocument("$not", regex)));
        }

        public QueryNotConditionList Size(
            BsonValue value
        ) {
            return new QueryNotConditionList(name, "$size", value);
        }

        public QueryNotConditionList Type(
            BsonType type
        ) {
            return new QueryNotConditionList(name, "$type", (int) type);
        }
        #endregion
    }

    [Serializable]
    public class QueryNotConditionList : QueryComplete {
        #region private fields
        private BsonDocument conditions;
        #endregion

        #region constructors
        public QueryNotConditionList(
            string name,
            string op,
            BsonValue value
        )
            : base(new BsonDocument(name, new BsonDocument("$not", new BsonDocument(op, value)))) {
            conditions = document[0].AsBsonDocument[0].AsBsonDocument;
        }
        #endregion

        #region public methods
        public QueryNotConditionList All(
            BsonArray array
        ) {
            conditions.Add("$all", array);
            return this;
        }

        public QueryNotConditionList All(
            params BsonValue[] values
        ) {
            conditions.Add("$all", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        public QueryNotConditionList ElemMatch(
            QueryComplete query
        ) {
            conditions.Add("$elemMatch", query.ToBsonDocument());
            return this;
        }

        public QueryNotConditionList Exists(
            bool value
        ) {
            conditions.Add("$exists", BsonBoolean.Create(value));
            return this;
        }

        public QueryNotConditionList GT(
            BsonValue value
        ) {
            conditions.Add("$gt", value);
            return this;
        }

        public QueryNotConditionList GTE(
            BsonValue value
        ) {
            conditions.Add("$gte", value);
            return this;
        }

        public QueryNotConditionList In(
            BsonArray array
        ) {
            conditions.Add("$in", array);
            return this;
        }

        public QueryNotConditionList In(
            params BsonValue[] values
        ) {
            conditions.Add("$in", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        public QueryNotConditionList LT(
            BsonValue value
        ) {
            conditions.Add("$lt", value);
            return this;
        }

        public QueryNotConditionList LTE(
            BsonValue value
        ) {
            conditions.Add("$lte", value);
            return this;
        }

        public QueryNotConditionList Mod(
            int modulus,
            int equals
        ) {
            conditions.Add("$mod", new BsonArray { modulus, equals });
            return this;
        }

        public QueryNotConditionList NE(
            BsonValue value
        ) {
            conditions.Add("$ne", value);
            return this;
        }

        public QueryNotConditionList NotIn(
            BsonArray array
        ) {
            conditions.Add("$nin", array);
            return this;
        }

        public QueryNotConditionList NotIn(
            params BsonValue[] values
        ) {
            conditions.Add("$nin", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        public QueryNotConditionList Size(
            int size
        ) {
            conditions.Add("$size", size);
            return this;
        }

        public QueryNotConditionList Type(
            BsonType type
        ) {
            conditions.Add("$type", (int) type);
            return this;
        }
        #endregion
    }
}
