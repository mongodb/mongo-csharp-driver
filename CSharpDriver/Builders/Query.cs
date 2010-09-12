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
        public static QueryBuilder and(
            params QueryBuilder[] queries
        ) {
            var document = new BsonDocument();
            foreach (var query in queries) {
                document.Add(query.Document);
            }
            return new QueryBuilder(document);
        }

        public static QueryBuilder.ElementStart Element(
            string name
        ) {
            return new QueryBuilder.ElementStart(name);
        }

        public static QueryBuilder or(
            params QueryBuilder[] queries
        ) {
            var document = new BsonDocument("$or", new BsonArray());
            foreach (var query in queries) {
                document[0].AsBsonArray.Add(query.Document);
            }
            return new QueryBuilder(document);
        }

        public static QueryBuilder where(
            BsonJavaScript javaScript
        ) {
            var document = new BsonDocument("$where", javaScript);
            return new QueryBuilder(document);
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

        #region public properties
        public BsonDocument Document {
            get { return document; }
        }
        #endregion

        #region public operators
        public static implicit operator BsonDocument(
            QueryBuilder builder
        ) {
            return builder.document;
        }
        #endregion

        #region nested classes
        public class Element : QueryBuilder {
            #region constructors
            public Element(
                BsonDocument document
            )
                : base(document) {
            }
            #endregion

            #region public methods
            public Element all(
                BsonArray array
            ) {
                document[0].AsBsonDocument.Add("$all", array);
                return this;
            }

            public Element all(
                params BsonValue[] values
            ) {
                document[0].AsBsonDocument.Add("$all", new BsonArray(values));
                return this;
            }

            public Element elemMatch(
                QueryBuilder query
            ) {
                document[0].AsBsonDocument.Add("$elemMatch", query.Document);
                return this;
            }

            public Element exists(
                bool value
            ) {
                document[0].AsBsonDocument.Add("$exists", BsonBoolean.Create(value));
                return this;
            }

            public Element gt(
                BsonValue value
            ) {
                document[0].AsBsonDocument.Add("$gt", value);
                return this;
            }

            public Element gte(
                BsonValue value
            ) {
                document[0].AsBsonDocument.Add("$gte", value);
                return this;
            }

            public Element @in(
                BsonArray array
            ) {
                document[0].AsBsonDocument.Add("$in", array);
                return this;
            }

            public Element @in(
                params BsonValue[] values
            ) {
                document[0].AsBsonDocument.Add("$in", new BsonArray(values));
                return this;
            }

            public Element lt(
                BsonValue value
            ) {
                document[0].AsBsonDocument.Add("$lt", value);
                return this;
            }

            public Element lte(
                BsonValue value
            ) {
                document[0].AsBsonDocument.Add("$lte", value);
                return this;
            }

            public Element mod(
                int modulus,
                int equals
            ) {
                document[0].AsBsonDocument.Add("$mod", new BsonArray { modulus, equals });
                return this;
            }

            public Element ne(
                BsonValue value
            ) {
                document[0].AsBsonDocument.Add("$ne", value);
                return this;
            }

            public Element nin(
                BsonArray array
            ) {
                document[0].AsBsonDocument.Add("$nin", array);
                return this;
            }

            public Element nin(
                params BsonValue[] values
            ) {
                document[0].AsBsonDocument.Add("$nin", new BsonArray(values));
                return this;
            }

            public Element size(
                int size
            ) {
                document[0].AsBsonDocument.Add("$size", size);
                return this;
            }

            public Element type(
                BsonType type
            ) {
                document[0].AsBsonDocument.Add("$type", (int) type);
                return this;
            }
            #endregion
        }

        public class ElementStart : Element {
            #region constructors
            public ElementStart(
                string name
            )
                : base(new BsonDocument(name, new BsonDocument())) {
            }
            #endregion

            #region public methods
            public QueryBuilder eq(
                BsonValue value
            ) {
                document[0] = value;
                return this;
            }

            public QueryBuilder regex(
               BsonRegularExpression value
           ) {
                document[0] = value;
                return this;
            }
            #endregion
        }
        #endregion
    }
}
