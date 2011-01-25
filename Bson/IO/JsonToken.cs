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
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using System.Xml;

namespace MongoDB.Bson.IO {
    public enum JsonTokenType {
        Invalid,
        BeginArray,
        BeginObject,
        EndArray,
        EndObject,
        Colon,
        Comma,
        DateTime,
        Double,
        Int32,
        Int64,
        ObjectId,
        RegularExpression,
        String,
        UnquotedString,
        EndOfFile
    }

    public class JsonToken {
        #region private fields
        private JsonTokenType type;
        private string lexeme;
        #endregion

        #region constructors
        public JsonToken(
            JsonTokenType type,
            string lexeme
        ) {
            this.type = type;
            this.lexeme = lexeme;
        }
        #endregion

        #region public properties
        public JsonTokenType Type {
            get { return type; }
        }

        public string Lexeme {
            get { return lexeme; }
        }

        public virtual DateTime DateTimeValue {
            get { throw new InvalidOperationException(); }
        }

        public virtual double DoubleValue {
            get { throw new InvalidOperationException(); }
        }

        public virtual int Int32Value {
            get { throw new InvalidOperationException(); }
        }

        public virtual long Int64Value {
            get { throw new InvalidOperationException(); }
        }

        public virtual ObjectId ObjectIdValue {
            get { throw new InvalidOperationException(); }
        }

        public virtual BsonRegularExpression RegularExpressionValue {
            get { throw new InvalidOperationException(); }
        }

        public virtual string StringValue {
            get { throw new InvalidOperationException(); }
        }
        #endregion
    }

    public class DateTimeJsonToken : JsonToken {
        #region private fields
        private DateTime value;
        #endregion

        #region constructors
        public DateTimeJsonToken(
            string lexeme,
            DateTime value
        )
            : base(JsonTokenType.DateTime, lexeme) {
            this.value = value;
        }
        #endregion

        #region public properties
        public override DateTime DateTimeValue {
            get { return value; }
        }
        #endregion
    }

    public class DoubleJsonToken : JsonToken {
        #region private fields
        private double value;
        #endregion

        #region constructors
        public DoubleJsonToken(
            string lexeme,
            double value
        )
            : base(JsonTokenType.Double, lexeme) {
            this.value = value;
        }
        #endregion

        #region public properties
        public override double DoubleValue {
            get { return value; }
        }
        #endregion
    }

    public class Int32JsonToken : JsonToken {
        #region private fields
        private int value;
        #endregion

        #region constructors
        public Int32JsonToken(
            string lexeme,
            int value
        )
            : base(JsonTokenType.Int32, lexeme) {
            this.value = value;
        }
        #endregion

        #region public properties
        public override int Int32Value {
            get { return value; }
        }

        public override long Int64Value {
            get { return value; }
        }
        #endregion
    }

    public class Int64JsonToken : JsonToken {
        #region private fields
        private long value;
        #endregion

        #region constructors
        public Int64JsonToken(
            string lexeme,
            long value
        )
            : base(JsonTokenType.Int64, lexeme) {
            this.value = value;
        }
        #endregion

        #region public properties
        public override long Int64Value {
            get { return value; }
        }
        #endregion
    }

    public class ObjectIdJsonToken : JsonToken {
        #region private fields
        private ObjectId value;
        #endregion

        #region constructors
        public ObjectIdJsonToken(
            string lexeme,
            ObjectId value
        )
            : base(JsonTokenType.ObjectId, lexeme) {
            this.value = value;
        }
        #endregion

        #region public properties
        public override ObjectId ObjectIdValue {
            get { return value; }
        }
        #endregion
    }

    public class RegularExpressionJsonToken : JsonToken {
        #region private fields
        private BsonRegularExpression value;
        #endregion

        #region constructors
        public RegularExpressionJsonToken(
            string lexeme,
            BsonRegularExpression value
        )
            : base(JsonTokenType.RegularExpression, lexeme) {
            this.value = value;
        }
        #endregion

        #region public properties
        public override BsonRegularExpression RegularExpressionValue {
            get { return value; }
        }
        #endregion
    }

    public class StringJsonToken : JsonToken {
        #region private fields
        private string value;
        #endregion

        #region constructors
        public StringJsonToken(
            JsonTokenType type,
            string lexeme,
            string value
        )
            : base(type, lexeme) {
            this.value = value;
        }
        #endregion

        #region public properties
        public override string StringValue {
            get { return value; }
        }
        #endregion
    }
}
