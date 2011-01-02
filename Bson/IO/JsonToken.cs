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
        Integer,
        FloatingPoint,
        String,
        UnquotedString,
        RegularExpression,
        EndOfFile
    }

    public class JsonToken {
        private JsonTokenType type;
        private string lexeme;
        private long integerValue;

        public JsonToken(
            JsonTokenType type,
            string lexeme
        ) {
            this.type = type;
            this.lexeme = lexeme;
        }

        public JsonTokenType Type {
            get { return type; }
        }

        public string Lexeme {
            get { return lexeme; }
        }

        public BsonType IntegerBsonType {
            get {
                integerValue = XmlConvert.ToInt64(lexeme);
                if (integerValue >= int.MinValue && integerValue <= int.MaxValue) {
                    return BsonType.Int32;
                } else {
                    return BsonType.Int64;
                }
            }
        }

        public long IntegerValue {
            get { return integerValue; }
        }
    }
}
