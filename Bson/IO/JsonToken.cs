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
using System.Xml;

using MongoDB.Bson;

namespace MongoDB.Bson.IO {
    /// <summary>
    /// Represents a JSON token type.
    /// </summary>
    public enum JsonTokenType {
        /// <summary>
        /// An invalid token.
        /// </summary>
        Invalid,
        /// <summary>
        /// A begin array token (a '[').
        /// </summary>
        BeginArray,
        /// <summary>
        /// A begin object token (a '{').
        /// </summary>
        BeginObject,
        /// <summary>
        /// An end array token (a ']').
        /// </summary>
        EndArray,
        /// <summary>
        /// A left parenthesis (a '(').
        /// </summary>
        LeftParen,
        /// <summary>
        /// A right parenthesis (a ')').
        /// </summary>
        RightParen,
        /// <summary>
        /// An end object token (a '}').
        /// </summary>
        EndObject,
        /// <summary>
        /// A colon token (a ':').
        /// </summary>
        Colon,
        /// <summary>
        /// A comma token (a ',').
        /// </summary>
        Comma,
        /// <summary>
        /// A DateTime token.
        /// </summary>
        DateTime,
        /// <summary>
        /// A Double token.
        /// </summary>
        Double,
        /// <summary>
        /// An Int32 token.
        /// </summary>
        Int32,
        /// <summary>
        /// And Int64 token.
        /// </summary>
        Int64,
        /// <summary>
        /// An ObjectId token.
        /// </summary>
        ObjectId,
        /// <summary>
        /// A regular expression token.
        /// </summary>
        RegularExpression,
        /// <summary>
        /// A string token.
        /// </summary>
        String,
        /// <summary>
        /// An unquoted string token.
        /// </summary>
        UnquotedString,
        /// <summary>
        /// An end of file token.
        /// </summary>
        EndOfFile
    }

    /// <summary>
    /// Represents a JSON token.
    /// </summary>
    public class JsonToken {
        #region private fields
        private JsonTokenType type;
        private string lexeme;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the JsonToken class.
        /// </summary>
        /// <param name="type">The token type.</param>
        /// <param name="lexeme">The lexeme.</param>
        public JsonToken(
            JsonTokenType type,
            string lexeme
        ) {
            this.type = type;
            this.lexeme = lexeme;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the token type.
        /// </summary>
        public JsonTokenType Type {
            get { return type; }
        }

        /// <summary>
        /// Gets the lexeme.
        /// </summary>
        public string Lexeme {
            get { return lexeme; }
        }

        /// <summary>
        /// Gets the value of a DateTime token.
        /// </summary>
        public virtual BsonDateTime DateTimeValue {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets the value of a Double token.
        /// </summary>
        public virtual double DoubleValue {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets the value of an Int32 token.
        /// </summary>
        public virtual int Int32Value {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets the value of an Int64 token.
        /// </summary>
        public virtual long Int64Value {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets the value of an ObjectId token.
        /// </summary>
        public virtual ObjectId ObjectIdValue {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets the value of a regular expression token.
        /// </summary>
        public virtual BsonRegularExpression RegularExpressionValue {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets the value of a string token.
        /// </summary>
        public virtual string StringValue {
            get { throw new NotSupportedException(); }
        }
        #endregion
    }

    /// <summary>
    /// Represents a DateTime JSON token.
    /// </summary>
    public class DateTimeJsonToken : JsonToken {
        #region private fields
        private BsonDateTime value;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the DateTimeJsonToken class.
        /// </summary>
        /// <param name="lexeme">The lexeme.</param>
        /// <param name="value">The DateTime value.</param>
        public DateTimeJsonToken(
            string lexeme,
            BsonDateTime value
        )
            : base(JsonTokenType.DateTime, lexeme) {
            this.value = value;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the value of a DateTime token.
        /// </summary>
        public override BsonDateTime DateTimeValue {
            get { return value; }
        }
        #endregion
    }

    /// <summary>
    /// Represents a Double JSON token.
    /// </summary>
    public class DoubleJsonToken : JsonToken {
        #region private fields
        private double value;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the DoubleJsonToken class.
        /// </summary>
        /// <param name="lexeme">The lexeme.</param>
        /// <param name="value">The Double value.</param>
        public DoubleJsonToken(
            string lexeme,
            double value
        )
            : base(JsonTokenType.Double, lexeme) {
            this.value = value;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the value of a Double token.
        /// </summary>
        public override double DoubleValue {
            get { return value; }
        }
        #endregion
    }

    /// <summary>
    /// Represents an Int32 JSON token.
    /// </summary>
    public class Int32JsonToken : JsonToken {
        #region private fields
        private int value;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the Int32JsonToken class.
        /// </summary>
        /// <param name="lexeme">The lexeme.</param>
        /// <param name="value">The Int32 value.</param>
        public Int32JsonToken(
            string lexeme,
            int value
        )
            : base(JsonTokenType.Int32, lexeme) {
            this.value = value;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the value of an Int32 token.
        /// </summary>
        public override int Int32Value {
            get { return value; }
        }

        /// <summary>
        /// Gets the value of an Int32 token as an Int64.
        /// </summary>
        public override long Int64Value {
            get { return value; }
        }
        #endregion
    }

    /// <summary>
    /// Represents an Int64 JSON token.
    /// </summary>
    public class Int64JsonToken : JsonToken {
        #region private fields
        private long value;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the Int64JsonToken class.
        /// </summary>
        /// <param name="lexeme">The lexeme.</param>
        /// <param name="value">The Int64 value.</param>
        public Int64JsonToken(
            string lexeme,
            long value
        )
            : base(JsonTokenType.Int64, lexeme) {
            this.value = value;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the value of an Int64 token.
        /// </summary>
        public override long Int64Value {
            get { return value; }
        }
        #endregion
    }

    /// <summary>
    /// Represents an ObjectId JSON token.
    /// </summary>
    public class ObjectIdJsonToken : JsonToken {
        #region private fields
        private ObjectId value;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the ObjectIdJsonToken class.
        /// </summary>
        /// <param name="lexeme">The lexeme.</param>
        /// <param name="value">The ObjectId value.</param>
        public ObjectIdJsonToken(
            string lexeme,
            ObjectId value
        )
            : base(JsonTokenType.ObjectId, lexeme) {
            this.value = value;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the value of an ObjectId token.
        /// </summary>
        public override ObjectId ObjectIdValue {
            get { return value; }
        }
        #endregion
    }

    /// <summary>
    /// Represents a regular expression JSON token.
    /// </summary>
    public class RegularExpressionJsonToken : JsonToken {
        #region private fields
        private BsonRegularExpression value;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the RegularExpressionJsonToken class.
        /// </summary>
        /// <param name="lexeme">The lexeme.</param>
        /// <param name="value">The BsonRegularExpression value.</param>
        public RegularExpressionJsonToken(
            string lexeme,
            BsonRegularExpression value
        )
            : base(JsonTokenType.RegularExpression, lexeme) {
            this.value = value;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the value of a regular expression token.
        /// </summary>
        public override BsonRegularExpression RegularExpressionValue {
            get { return value; }
        }
        #endregion
    }

    /// <summary>
    /// Represents a String JSON token.
    /// </summary>
    public class StringJsonToken : JsonToken {
        #region private fields
        private string value;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the StringJsonToken class.
        /// </summary>
        /// <param name="type">The token type.</param>
        /// <param name="lexeme">The lexeme.</param>
        /// <param name="value">The String value.</param>
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
        /// <summary>
        /// Gets the value of an String token.
        /// </summary>
        public override string StringValue {
            get { return value; }
        }
        #endregion
    }
}
