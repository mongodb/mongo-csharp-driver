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

using MongoDB.BsonLibrary;
using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;

namespace MongoDB.CSharpDriver.Builders {
    public static class Query {
        #region public static methods
        public static QueryConditionList all(
            string name,
            BsonArray array
        ) {
            return new QueryConditionList(name, "$all", array);
        }

        public static QueryConditionList all(
            string name,
            params BsonValue[] values
        ) {
            return new QueryConditionList(name, "$all", new BsonArray((IEnumerable<BsonValue>) values));
        }

        public static QueryComplete and(
            params QueryComplete[] queries
        ) {
            var document = new BsonDocument();
            foreach (var query in queries) {
                document.Add(query.ToBsonDocument());
            }
            return new QueryComplete(document);
        }

        public static QueryConditionList elemMatch(
            string name,
            QueryComplete query
        ) {
            return new QueryConditionList(name, "$elemMatch", query.ToBsonDocument());
        }

        public static QueryComplete eq(
            string name,
            BsonValue value
        ) {
            return new QueryComplete(new BsonDocument(name, value));
        }

        public static QueryConditionList exists(
            string name,
            bool value
        ) {
            return new QueryConditionList(name, "$exists", BsonBoolean.Create(value));
        }

        public static QueryConditionList gt(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name, "$gt", value);
        }

        public static QueryConditionList gte(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name, "$gte", value);
        }

        public static QueryConditionList @in(
            string name,
            BsonArray value
        ) {
            return new QueryConditionList(name, "$in", value);
        }

        public static QueryConditionList @in(
            string name,
            params BsonValue[] values
        ) {
            return new QueryConditionList(name, "$in", new BsonArray((IEnumerable<BsonValue>) values));
        }

        public static QueryConditionList lt(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name, "$lt", value);
        }

        public static QueryConditionList lte(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name, "$lte", value);
        }

        public static QueryComplete matches(
            string name,
            BsonRegularExpression regex
        ) {
            return new QueryComplete(new BsonDocument(name, regex));
        }

        public static QueryConditionList mod(
            string name,
            int modulus,
            int equals
        ) {
            return new QueryConditionList(name, "$mod", new BsonArray { modulus, equals });
        }

        public static QueryConditionList ne(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name, "$ne", value);
        }

        public static QueryConditionList nin(
            string name,
            BsonArray array
        ) {
            return new QueryConditionList(name, "$nin", array);
        }

        public static QueryConditionList nin(
            string name,
            params BsonValue[] values
        ) {
            return new QueryConditionList(name, "$nin", new BsonArray((IEnumerable<BsonValue>) values));
        }

        public static QueryNot not(
            string name
        ) {
            return new QueryNot(name);
        }

        public static QueryComplete or(
            params QueryComplete[] queries
        ) {
            var array = new BsonArray();
            foreach (var query in queries) {
                array.Add(query.ToBsonDocument());
            }
            var document = new BsonDocument("$or", array);
            return new QueryComplete(document);
        }

        public static QueryConditionList size(
            string name,
            int size
        ) {
            return new QueryConditionList(name, "$size", size);
        }

        public static QueryConditionList type(
            string name,
            BsonType type
        ) {
            return new QueryConditionList(name, "$type", (int) type);
        }

        public static QueryComplete where(
            BsonJavaScript javaScript
        ) {
            return new QueryComplete(new BsonDocument("$where", javaScript));
        }
        #endregion
    }

    public abstract class QueryBuilder : BuilderBase, IBsonDocumentBuilder, IBsonSerializable {
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
        public BsonDocument ToBsonDocument() {
            return document;
        }
        #endregion

        #region explicit interface implementations
        void IBsonSerializable.Deserialize(
            BsonReader bsonReader
        ) {
            throw new InvalidOperationException("Deserialize is not supported for OrderByBuilder");
        }

        void IBsonSerializable.Serialize(
            BsonWriter bsonWriter,
            bool serializeIdFirst
        ) {
            document.Serialize(bsonWriter, serializeIdFirst);
        }
        #endregion
    }

    public class QueryComplete : QueryBuilder {
        #region constructors
        public QueryComplete(
            BsonDocument document
        )
            : base(document) {
        }
        #endregion
    }

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
        public QueryConditionList all(
            BsonArray array
        ) {
            conditions.Add("$all", array);
            return this;
        }

        public QueryConditionList all(
            params BsonValue[] values
        ) {
            conditions.Add("$all", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        public QueryConditionList elemMatch(
            QueryComplete query
        ) {
            conditions.Add("$elemMatch", query.ToBsonDocument());
            return this;
        }

        public QueryConditionList exists(
            bool value
        ) {
            conditions.Add("$exists", BsonBoolean.Create(value));
            return this;
        }

        public QueryConditionList gt(
            BsonValue value
        ) {
            conditions.Add("$gt", value);
            return this;
        }

        public QueryConditionList gte(
            BsonValue value
        ) {
            conditions.Add("$gte", value);
            return this;
        }

        public QueryConditionList @in(
            BsonArray array
        ) {
            conditions.Add("$in", array);
            return this;
        }

        public QueryConditionList @in(
            params BsonValue[] values
        ) {
            conditions.Add("$in", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        public QueryConditionList lt(
            BsonValue value
        ) {
            conditions.Add("$lt", value);
            return this;
        }

        public QueryConditionList lte(
            BsonValue value
        ) {
            conditions.Add("$lte", value);
            return this;
        }

        public QueryConditionList mod(
            int modulus,
            int equals
        ) {
            conditions.Add("$mod", new BsonArray { modulus, equals });
            return this;
        }

        public QueryConditionList ne(
            BsonValue value
        ) {
            conditions.Add("$ne", value);
            return this;
        }

        public QueryConditionList nin(
            BsonArray array
        ) {
            conditions.Add("$nin", array);
            return this;
        }

        public QueryConditionList nin(
            params BsonValue[] values
        ) {
            conditions.Add("$nin", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        public QueryConditionList size(
            int size
        ) {
            conditions.Add("$size", size);
            return this;
        }

        public QueryConditionList type(
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
        public QueryNotConditionList all(
            BsonArray array
        ) {
            return new QueryNotConditionList(name, "$all", array);
        }

        public QueryNotConditionList all(
            params BsonValue[] values
        ) {
            return new QueryNotConditionList(name, "$all", new BsonArray((IEnumerable<BsonValue>) values));
        }

        public QueryNotConditionList elemMatch(
            QueryComplete query
        ) {
            return new QueryNotConditionList(name, "$elemMatch", query.ToBsonDocument());
        }

        public QueryNotConditionList exists(
            bool value
        ) {
            return new QueryNotConditionList(name, "$exists", BsonBoolean.Create(value));
        }

        public QueryNotConditionList gt(
            BsonValue value
        ) {
            return new QueryNotConditionList(name, "$gt", value);
        }

        public QueryNotConditionList gte(
            BsonValue value
        ) {
            return new QueryNotConditionList(name, "$gte", value);
        }

        public QueryNotConditionList @in(
            BsonArray array
        ) {
            return new QueryNotConditionList(name, "$in", array);
        }

        public QueryNotConditionList @in(
            params BsonValue[] values
        ) {
            return new QueryNotConditionList(name, "$in", new BsonArray((IEnumerable<BsonValue>) values));
        }

        public QueryNotConditionList mod(
            int modulus,
            int equals
        ) {
            return new QueryNotConditionList(name, "$mod", new BsonArray { modulus, equals });
        }

        public QueryNotConditionList ne(
            BsonValue value
        ) {
            return new QueryNotConditionList(name, "$ne", value);
        }

        public QueryNotConditionList nin(
            BsonArray array
        ) {
            return new QueryNotConditionList(name, "nin", array);
        }

        public QueryNotConditionList nin(
            params BsonValue[] values
        ) {
            return new QueryNotConditionList(name, "nin", new BsonArray((IEnumerable<BsonValue>) values));
        }

        public QueryNotConditionList lt(
            BsonValue value
        ) {
            return new QueryNotConditionList(name, "$lt", value);
        }

        public QueryNotConditionList lte(
            BsonValue value
        ) {
            return new QueryNotConditionList(name, "$lte", value);
        }

        public QueryComplete matches(
            BsonRegularExpression regex
        ) {
            return new QueryComplete(new BsonDocument(name, new BsonDocument("$not", regex)));
        }

        public QueryNotConditionList size(
            BsonValue value
        ) {
            return new QueryNotConditionList(name, "$size", value);
        }

        public QueryNotConditionList type(
            BsonType type
        ) {
            return new QueryNotConditionList(name, "$type", (int) type);
        }
        #endregion
    }

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
        public QueryNotConditionList all(
            BsonArray array
        ) {
            conditions.Add("$all", array);
            return this;
        }

        public QueryNotConditionList all(
            params BsonValue[] values
        ) {
            conditions.Add("$all", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        public QueryNotConditionList elemMatch(
            QueryComplete query
        ) {
            conditions.Add("$elemMatch", query.ToBsonDocument());
            return this;
        }

        public QueryNotConditionList exists(
            bool value
        ) {
            conditions.Add("$exists", BsonBoolean.Create(value));
            return this;
        }

        public QueryNotConditionList gt(
            BsonValue value
        ) {
            conditions.Add("$gt", value);
            return this;
        }

        public QueryNotConditionList gte(
            BsonValue value
        ) {
            conditions.Add("$gte", value);
            return this;
        }

        public QueryNotConditionList @in(
            BsonArray array
        ) {
            conditions.Add("$in", array);
            return this;
        }

        public QueryNotConditionList @in(
            params BsonValue[] values
        ) {
            conditions.Add("$in", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        public QueryNotConditionList lt(
            BsonValue value
        ) {
            conditions.Add("$lt", value);
            return this;
        }

        public QueryNotConditionList lte(
            BsonValue value
        ) {
            conditions.Add("$lte", value);
            return this;
        }

        public QueryNotConditionList mod(
            int modulus,
            int equals
        ) {
            conditions.Add("$mod", new BsonArray { modulus, equals });
            return this;
        }

        public QueryNotConditionList ne(
            BsonValue value
        ) {
            conditions.Add("$ne", value);
            return this;
        }

        public QueryNotConditionList nin(
            BsonArray array
        ) {
            conditions.Add("$nin", array);
            return this;
        }

        public QueryNotConditionList nin(
            params BsonValue[] values
        ) {
            conditions.Add("$nin", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        public QueryNotConditionList size(
            int size
        ) {
            conditions.Add("$size", size);
            return this;
        }

        public QueryNotConditionList type(
            BsonType type
        ) {
            conditions.Add("$type", (int) type);
            return this;
        }
        #endregion
    }
}
