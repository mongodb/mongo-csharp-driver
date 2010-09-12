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
using MongoDB.CSharpDriver;

namespace MongoDB.CSharpDriver.Builders {
    public static class Fields {
        #region public static methods
        public static FieldsBuilder Exclude(
            params string[] names
        ) {
            var builder = new FieldsBuilder();
            builder.Exclude(names);
            return builder;
        }

        public static FieldsBuilder Include(
            params string[] names
        ) {
            var builder = new FieldsBuilder();
            builder.Include(names);
            return builder;
        }

        public static FieldsBuilder Slice(
            string name,
            int size // negative sizes are from the end
        ) {
            var builder = new FieldsBuilder();
            builder.Slice(name, size);
            return builder;
        }

        public static FieldsBuilder Slice(
            string name,
            int skip,
            int limit
        ) {
            var builder = new FieldsBuilder();
            builder.Slice(name, skip, limit);
            return builder;
        }
        #endregion
    }

    public class FieldsBuilder {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        public FieldsBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public operators
        public static implicit operator BsonDocument(
            FieldsBuilder builder
        ) {
            return builder.document;
        }
        #endregion

        #region public methods
        public FieldsBuilder Exclude(
            params string[] names
        ) {
            foreach (var name in names) {
                document.Add(name, 0);
            }
            return this;
        }

        public FieldsBuilder Include(
            params string[] names
        ) {
            foreach (var name in names) {
                document.Add(name, 1);
            }
            return this;
        }

        public FieldsBuilder Slice(
            string name,
            int size // negative sizes are from the end
        ) {
            document.Add(name, new BsonDocument("$slice", size));
            return this;
        }

        public FieldsBuilder Slice(
            string name,
            int skip,
            int limit
        ) {
            document.Add(name, new BsonDocument("$slice", new BsonArray { skip, limit }));
            return this;
        }
        #endregion
    }
}
