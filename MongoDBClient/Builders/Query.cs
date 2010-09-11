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

namespace MongoDB.MongoDBClient.Builders {
    public static class Query {
        #region public static methods
        public static QueryElementFragment All(
            string name,
            BsonArray array
        ) {
            var document = new BsonDocument(name, new BsonDocument("$all", array));
            return new QueryElementFragment(document);
        }

        public static QueryElementFragment All(
            string name,
            params BsonValue[] values
        ) {
            var document = new BsonDocument(name, new BsonDocument("$all", new BsonArray(values)));
            return new QueryElementFragment(document);
        }

        public static QueryFragment And(
            params QueryFragment[] fragments
        ) {
            var document = new BsonDocument();
            foreach (var fragment in fragments) {
                document.Add((BsonDocument) fragment);
            }
            return new QueryFragment(document);
        }

        public static QueryFragment Eq(
            string name,
            BsonValue value
        ) {
            var document = new BsonDocument(name, value);
            return new QueryFragment(document);
        }

        public static QueryElementFragment GT(
            string name,
            BsonValue value
        ) {
            var document = new BsonDocument(name, new BsonDocument("$gt", value));
            return new QueryElementFragment(document);
        }

        public static QueryElementFragment GTE(
            string name,
            BsonValue value
        ) {
            var document = new BsonDocument(name, new BsonDocument("$gte", value));
            return new QueryElementFragment(document);
        }

        public static QueryElementFragment In(
            string name,
            BsonArray array
        ) {
            var document = new BsonDocument(name, new BsonDocument("$in", array));
            return new QueryElementFragment(document);
        }

        public static QueryElementFragment In(
            string name,
            params BsonValue[] values
        ) {
            var document = new BsonDocument(name, new BsonDocument("$in", new BsonArray(values)));
            return new QueryElementFragment(document);
        }

        public static QueryElementFragment LT(
            string name,
            BsonValue value
        ) {
            var document = new BsonDocument(name, new BsonDocument("$lt", value));
            return new QueryElementFragment(document);
        }

        public static QueryElementFragment LTE(
            string name,
            BsonValue value
        ) {
            var document = new BsonDocument(name, new BsonDocument("$lte", value));
            return new QueryElementFragment(document);
        }

        public static QueryElementFragment Mod(
            string name,
            int modulus,
            int equals
        ) {
            var document = new BsonDocument(name, new BsonDocument("$mod", new BsonArray { modulus, equals }));
            return new QueryElementFragment(document);
        }

        public static QueryElementFragment NE(
            string name,
            BsonValue value
        ) {
            var document = new BsonDocument(name, new BsonDocument("$ne", value));
            return new QueryElementFragment(document);
        }

        public static QueryElementFragment NotIn(
            string name,
            BsonArray array
        ) {
            var document = new BsonDocument(name, new BsonDocument("$nin", array));
            return new QueryElementFragment(document);
        }

        public static QueryElementFragment NotIn(
            string name,
            params BsonValue[] values
        ) {
            var document = new BsonDocument(name, new BsonDocument("$nin", new BsonArray(values)));
            return new QueryElementFragment(document);
        }

        public static QueryElementFragment Size(
            string name,
            int size
        ) {
            var document = new BsonDocument(name, new BsonDocument("$size", size));
            return new QueryElementFragment(document);
        }
        #endregion
    }

    public class QueryFragment {
        #region private fields
        protected BsonDocument document;
        #endregion

        #region constructors
        public QueryFragment(
            BsonDocument document
        ) {
            this.document = document;
        }
        #endregion

        #region public operators
        public static implicit operator BsonDocument(
            QueryFragment fragment
        ) {
            return fragment.document;
        }
        #endregion
    }

    public class QueryElementFragment : QueryFragment {
        #region constructors
        public QueryElementFragment(
            BsonDocument document
        )
            : base(document) {
        }
        #endregion

        #region public methods
        public QueryElementFragment All(
            BsonArray array
        ) {
            document[0].AsBsonDocument.Add("$all", array);
            return this;
        }

        public QueryElementFragment All(
            params BsonValue[] values
        ) {
            document[0].AsBsonDocument.Add("$all", new BsonArray(values));
            return this;
        }

        public QueryElementFragment GT(
            BsonValue value
        ) {
            document[0].AsBsonDocument.Add("$gt", value);
            return this;
        }

        public QueryElementFragment GE(
            BsonValue value
        ) {
            document[0].AsBsonDocument.Add("$gte", value);
            return this;
        }

        public QueryElementFragment In(
            BsonArray array
        ) {
            document[0].AsBsonDocument.Add("$in", array);
            return this;
        }

        public QueryElementFragment In(
            params BsonValue[] values
        ) {
            document[0].AsBsonDocument.Add("$in", new BsonArray(values));
            return this;
        }

        public QueryElementFragment LT(
            BsonValue value
        ) {
            document[0].AsBsonDocument.Add("$lt", value);
            return this;
        }

        public QueryElementFragment LE(
            BsonValue value
        ) {
            document[0].AsBsonDocument.Add("$lte", value);
            return this;
        }

        public QueryElementFragment Mod(
            int modulus,
            int equals
        ) {
            document[0].AsBsonDocument.Add("$mod", new BsonArray { modulus, equals });
            return this;
        }

        public QueryElementFragment NE(
            BsonValue value
        ) {
            document[0].AsBsonDocument.Add("$ne", value);
            return this;
        }

        public QueryElementFragment NotIn(
            BsonArray array
        ) {
            document[0].AsBsonDocument.Add("$nin", array);
            return this;
        }

        public QueryElementFragment NotIn(
            params BsonValue[] values
        ) {
            document[0].AsBsonDocument.Add("$nin", new BsonArray(values));
            return this;
        }

        public QueryElementFragment Size(
            int size
        ) {
            document[0].AsBsonDocument.Add("$size", size);
            return this;
        }
        #endregion
    }
}
