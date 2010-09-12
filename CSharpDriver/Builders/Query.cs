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

namespace MongoDB.CSharpDriver.Builders {
    public static class Query {
        #region public static methods
        public static QueryElementBuilder All(
            string name,
            BsonArray array
        ) {
            var document = new BsonDocument(name, new BsonDocument("$all", array));
            return new QueryElementBuilder(document);
        }

        public static QueryElementBuilder All(
            string name,
            params BsonValue[] values
        ) {
            var document = new BsonDocument(name, new BsonDocument("$all", new BsonArray(values)));
            return new QueryElementBuilder(document);
        }

        public static QueryBuilder And(
            params QueryBuilder[] queries
        ) {
            var document = new BsonDocument();
            foreach (var query in queries) {
                document.Add((BsonDocument) query);
            }
            return new QueryBuilder(document);
        }

        public static QueryBuilder Eq(
            string name,
            BsonValue value
        ) {
            var document = new BsonDocument(name, value);
            return new QueryBuilder(document);
        }

        public static QueryElementBuilder GT(
            string name,
            BsonValue value
        ) {
            var document = new BsonDocument(name, new BsonDocument("$gt", value));
            return new QueryElementBuilder(document);
        }

        public static QueryElementBuilder GTE(
            string name,
            BsonValue value
        ) {
            var document = new BsonDocument(name, new BsonDocument("$gte", value));
            return new QueryElementBuilder(document);
        }

        public static QueryElementBuilder In(
            string name,
            BsonArray array
        ) {
            var document = new BsonDocument(name, new BsonDocument("$in", array));
            return new QueryElementBuilder(document);
        }

        public static QueryElementBuilder In(
            string name,
            params BsonValue[] values
        ) {
            var document = new BsonDocument(name, new BsonDocument("$in", new BsonArray(values)));
            return new QueryElementBuilder(document);
        }

        public static QueryElementBuilder LT(
            string name,
            BsonValue value
        ) {
            var document = new BsonDocument(name, new BsonDocument("$lt", value));
            return new QueryElementBuilder(document);
        }

        public static QueryElementBuilder LTE(
            string name,
            BsonValue value
        ) {
            var document = new BsonDocument(name, new BsonDocument("$lte", value));
            return new QueryElementBuilder(document);
        }

        public static QueryElementBuilder Mod(
            string name,
            int modulus,
            int equals
        ) {
            var document = new BsonDocument(name, new BsonDocument("$mod", new BsonArray { modulus, equals }));
            return new QueryElementBuilder(document);
        }

        public static QueryElementBuilder NE(
            string name,
            BsonValue value
        ) {
            var document = new BsonDocument(name, new BsonDocument("$ne", value));
            return new QueryElementBuilder(document);
        }

        public static QueryElementBuilder NotIn(
            string name,
            BsonArray array
        ) {
            var document = new BsonDocument(name, new BsonDocument("$nin", array));
            return new QueryElementBuilder(document);
        }

        public static QueryElementBuilder NotIn(
            string name,
            params BsonValue[] values
        ) {
            var document = new BsonDocument(name, new BsonDocument("$nin", new BsonArray(values)));
            return new QueryElementBuilder(document);
        }

        public static QueryElementBuilder Size(
            string name,
            int size
        ) {
            var document = new BsonDocument(name, new BsonDocument("$size", size));
            return new QueryElementBuilder(document);
        }
        #endregion
    }

    public class QueryBuilder {
        #region private fields
        protected BsonDocument document;
        #endregion

        #region constructors
        public QueryBuilder(
            BsonDocument document
        ) {
            this.document = document;
        }
        #endregion

        #region public operators
        public static implicit operator BsonDocument(
            QueryBuilder builder
        ) {
            return builder.document;
        }
        #endregion
    }

    public class QueryElementBuilder : QueryBuilder {
        #region constructors
        public QueryElementBuilder(
            BsonDocument document
        )
            : base(document) {
        }
        #endregion

        #region public methods
        public QueryElementBuilder All(
            BsonArray array
        ) {
            document[0].AsBsonDocument.Add("$all", array);
            return this;
        }

        public QueryElementBuilder All(
            params BsonValue[] values
        ) {
            document[0].AsBsonDocument.Add("$all", new BsonArray(values));
            return this;
        }

        public QueryElementBuilder GT(
            BsonValue value
        ) {
            document[0].AsBsonDocument.Add("$gt", value);
            return this;
        }

        public QueryElementBuilder GE(
            BsonValue value
        ) {
            document[0].AsBsonDocument.Add("$gte", value);
            return this;
        }

        public QueryElementBuilder In(
            BsonArray array
        ) {
            document[0].AsBsonDocument.Add("$in", array);
            return this;
        }

        public QueryElementBuilder In(
            params BsonValue[] values
        ) {
            document[0].AsBsonDocument.Add("$in", new BsonArray(values));
            return this;
        }

        public QueryElementBuilder LT(
            BsonValue value
        ) {
            document[0].AsBsonDocument.Add("$lt", value);
            return this;
        }

        public QueryElementBuilder LE(
            BsonValue value
        ) {
            document[0].AsBsonDocument.Add("$lte", value);
            return this;
        }

        public QueryElementBuilder Mod(
            int modulus,
            int equals
        ) {
            document[0].AsBsonDocument.Add("$mod", new BsonArray { modulus, equals });
            return this;
        }

        public QueryElementBuilder NE(
            BsonValue value
        ) {
            document[0].AsBsonDocument.Add("$ne", value);
            return this;
        }

        public QueryElementBuilder NotIn(
            BsonArray array
        ) {
            document[0].AsBsonDocument.Add("$nin", array);
            return this;
        }

        public QueryElementBuilder NotIn(
            params BsonValue[] values
        ) {
            document[0].AsBsonDocument.Add("$nin", new BsonArray(values));
            return this;
        }

        public QueryElementBuilder Size(
            int size
        ) {
            document[0].AsBsonDocument.Add("$size", size);
            return this;
        }
        #endregion
    }
}
