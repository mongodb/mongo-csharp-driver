/* Copyright 2010-2011 10gen Inc.
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
            BsonArray values
        ) {
            return new QueryConditionList(name).All(values);
        }

        public static QueryConditionList All(
            string name,
            params BsonValue[] values
        ) {
            return new QueryConditionList(name).All(values);
        }

        public static QueryComplete And(
            params QueryComplete[] queries
        ) {
            var document = new BsonDocument();
            foreach (var query in queries) {
                foreach (var queryElement in query.ToBsonDocument()) {
                    // if result document has existing operations for same field append the new ones
                    if (document.Contains(queryElement.Name)) {
                        var existingOperations = document[queryElement.Name] as BsonDocument;
                        var newOperations = queryElement.Value as BsonDocument;

                        // make sure that no conditions are Query.EQ, because duplicates aren't allowed
                        if (existingOperations == null || newOperations == null) {
                            var message = string.Format("Query.And does not support combining equality comparisons with other operators (field: '{0}')", queryElement.Name);
                            throw new InvalidOperationException(message);
                        }

                        // add each new operation to the existing operations
                        foreach (var operation in newOperations) {
                            // make sure that there are no duplicate $operators
                            if (existingOperations.Contains(operation.Name)) {
                                var message = string.Format("Query.And does not support using the same operator more than once (field: '{0}', operator: '{1}')", queryElement.Name, operation.Name);
                                throw new InvalidOperationException(message);
                            } else {
                                existingOperations.Add(operation);
                            }
                        }
                    } else {
                        document.Add(queryElement);
                    }
                }
            }
            return new QueryComplete(document);
        }

        public static QueryConditionList ElemMatch(
            string name,
            QueryComplete query
        ) {
            return new QueryConditionList(name).ElemMatch(query);
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
            return new QueryConditionList(name).Exists(value);
        }

        public static QueryConditionList GT(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name).GT(value);
        }

        public static QueryConditionList GTE(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name).GTE(value);
        }

        public static QueryConditionList In(
            string name,
            BsonArray value
        ) {
            return new QueryConditionList(name).In(value);
        }

        public static QueryConditionList In(
            string name,
            params BsonValue[] values
        ) {
            return new QueryConditionList(name).In(values);
        }

        public static QueryConditionList LT(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name).LT(value);
        }

        public static QueryConditionList LTE(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name).LTE(value);
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
            return new QueryConditionList(name).Mod(modulus, equals);
        }

        public static QueryConditionList NE(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name).NE(value);
        }

        public static QueryConditionList Near(
            string name,
            double x,
            double y
        ) {
            return new QueryConditionList(name).Near(x, y);
        }

        public static QueryConditionList Near(
            string name,
            double x,
            double y,
            double maxDistance
        ) {
            return new QueryConditionList(name).Near(x, y, maxDistance);
        }

        public static QueryConditionList Near(
            string name,
            double x,
            double y,
            double maxDistance,
            bool spherical
        ) {
            return new QueryConditionList(name).Near(x, y, maxDistance, spherical);
        }

        public static QueryConditionList NotIn(
            string name,
            BsonArray values
        ) {
            return new QueryConditionList(name).NotIn(values);
        }

        public static QueryConditionList NotIn(
            string name,
            params BsonValue[] values
        ) {
            return new QueryConditionList(name).NotIn(values);
        }

        public static QueryNot Not(
            string name
        ) {
            return new QueryNot(name);
        }

        public static QueryComplete Or(
            params QueryComplete[] queries
        ) {
            var clauses = new BsonArray();
            foreach (var query in queries) {
                clauses.Add(query.ToBsonDocument());
            }
            var document = new BsonDocument("$or", clauses);
            return new QueryComplete(document);
        }

        public static QueryConditionList Size(
            string name,
            int size
        ) {
            return new QueryConditionList(name).Size(size);
        }

        public static QueryConditionList Type(
            string name,
            BsonType type
        ) {
            return new QueryConditionList(name).Type(type);
        }

        public static QueryComplete Where(
            BsonJavaScript javaScript
        ) {
            return new QueryComplete(new BsonDocument("$where", javaScript));
        }

        public static QueryConditionList WithinCircle(
            string name,
            double centerX,
            double centerY,
            double radius
        ) {
            return new QueryConditionList(name).WithinCircle(centerX, centerY, radius);
        }

        public static QueryConditionList WithinCircle(
            string name,
            double centerX,
            double centerY,
            double radius,
            bool spherical
        ) {
            return new QueryConditionList(name).WithinCircle(centerX, centerY, radius, spherical);
        }

        public static QueryConditionList WithinRectangle(
            string name,
            double lowerLeftX,
            double lowerLeftY,
            double upperRightX,
            double upperRightY
        ) {
            return new QueryConditionList(name).WithinRectangle(lowerLeftX, lowerLeftY, upperRightX, upperRightY);
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
            string name
        )
            : base(new BsonDocument(name, new BsonDocument())) {
            conditions = document[0].AsBsonDocument;
        }
        #endregion

        #region public methods
        public QueryConditionList All(
            BsonArray values
        ) {
            conditions.Add("$all", values);
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
            BsonArray values
        ) {
            conditions.Add("$in", values);
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

        public QueryConditionList Near(
            double x,
            double y
        ) {
            return Near(x, y, double.MaxValue);
        }

        public QueryConditionList Near(
            double x,
            double y,
            double maxDistance
        ) {
            return Near(x, y, maxDistance, false); // not spherical
        }

        public QueryConditionList Near(
            double x,
            double y,
            double maxDistance,
            bool spherical
        ) {
            var op = spherical ? "$nearSphere" : "$near";
            conditions.Add(op, new BsonArray { x, y });
            if (maxDistance != double.MaxValue) {
                conditions.Add("$maxDistance", maxDistance);
            }
            return this;
        }

        public QueryConditionList NotIn(
            BsonArray values
        ) {
            conditions.Add("$nin", values);
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

        public QueryConditionList WithinCircle(
            double x,
            double y,
            double radius
        ) {
            return WithinCircle(x, y, radius, false); // not spherical
        }

        public QueryConditionList WithinCircle(
            double x,
            double y,
            double radius,
            bool spherical
        ) {
            var shape = spherical ? "$centerSphere" : "$center";
            conditions.Add("$within", new BsonDocument(shape, new BsonArray { new BsonArray { x, y }, radius }));
            return this;
        }

        public QueryConditionList WithinRectangle(
            double lowerLeftX,
            double lowerLeftY,
            double upperRightX,
            double upperRightY
        ) {
            conditions.Add("$within", new BsonDocument("$box", new BsonArray { new BsonArray { lowerLeftX, lowerLeftY }, new BsonArray { upperRightX, upperRightY } }));
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
            BsonArray values
        ) {
            return new QueryNotConditionList(name, "$all", values);
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
            BsonArray values
        ) {
            return new QueryNotConditionList(name, "$in", values);
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
            BsonArray values
        ) {
            return new QueryNotConditionList(name, "nin", values);
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
            BsonArray values
        ) {
            conditions.Add("$all", values);
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
            BsonArray values
        ) {
            conditions.Add("$in", values);
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
            BsonArray values
        ) {
            conditions.Add("$nin", values);
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
