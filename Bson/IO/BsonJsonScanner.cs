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

    public static class BsonJsonScanner {
        #region public static methods
        public static JsonToken GetNextToken(
            BsonJsonBuffer buffer
        ) {
            // skip leading whitespace
            var c = buffer.Peek();
            while (c != -1 && char.IsWhiteSpace((char) c)) {
                buffer.Read(); // ignore whitespace
                c = buffer.Peek();
            }

            // check for end of file
            if (c == -1) {
                return new JsonToken(JsonTokenType.EndOfFile, "<eof>");
            }

            // check for single character tokens
            switch (c) {
                case '{': buffer.Read(); return new JsonToken(JsonTokenType.BeginObject, "{");
                case '}': buffer.Read(); return new JsonToken(JsonTokenType.EndObject, "}");
                case '[': buffer.Read(); return new JsonToken(JsonTokenType.BeginArray, "[");
                case ']': buffer.Read(); return new JsonToken(JsonTokenType.EndArray, "]");
                case ':': buffer.Read(); return new JsonToken(JsonTokenType.Colon, ":");
                case ',': buffer.Read(); return new JsonToken(JsonTokenType.Comma, ",");
            }

            // scan strings
            if (c == '"') {
                return GetStringToken(buffer);
            }

            // scan numbers
            if (c == '-' || char.IsDigit((char) c)) {
                return GetNumberToken(buffer);
            }

            // true, false and null are returned as unquoted strings and detected by the parser
            if (char.IsLetter((char) c)) {
                return GetUnquotedStringToken(buffer);
            }

            throw new FileFormatException("Invalid JSON input");
        }
        #endregion

        #region private methods
        private static JsonToken GetNumberToken(
            BsonJsonBuffer buffer
        ) {
            var state = NumberState.Initial;
            var type = JsonTokenType.Integer; // assume integer until proved otherwise
            var sb = new StringBuilder();
            while (true) {
                var c = buffer.Peek();
                switch (state) {
                    case NumberState.Initial:
                        switch (c) {
                            case '-':
                                state = NumberState.SawLeadingMinus;
                                break;
                            case '0': state =
                                NumberState.SawLeadingZero;
                                break;
                            default:
                                if (char.IsDigit((char) c)) {
                                    state = NumberState.SawIntegerDigits;
                                } else {
                                    state = NumberState.Invalid;
                                }
                                break;
                        }
                        break;
                    case NumberState.SawLeadingMinus:
                        switch (c) {
                            case '0':
                                state = NumberState.SawLeadingZero; 
                                break;
                            default:
                                if (char.IsDigit((char) c)) {
                                    state = NumberState.SawIntegerDigits;
                                } else {
                                    state = NumberState.Invalid;
                                }
                                break;
                        }
                        break;
                    case NumberState.SawLeadingZero:
                        switch (c) {
                            case '.':
                                state = NumberState.SawDecimalPoint;
                                break;
                            case 'e':
                            case 'E':
                                state = NumberState.SawExponentLetter;
                                break;
                            case ',':
                            case '}':
                            case ']':
                            case -1:
                                state = NumberState.Done;
                                break;
                            default:
                                if (char.IsWhiteSpace((char) c)) {
                                    state = NumberState.Done;
                                } else {
                                    state = NumberState.Invalid;
                                }
                                break;
                        }
                        break;
                    case NumberState.SawIntegerDigits:
                        switch (c) {
                            case '.':
                                state = NumberState.SawDecimalPoint;
                                break;
                            case 'e':
                            case 'E':
                                state = NumberState.SawExponentLetter;
                                break;
                            case ',':
                            case '}':
                            case ']':
                            case -1:
                                state = NumberState.Done;
                                break;
                            default:
                                if (char.IsDigit((char) c)) {
                                    state = NumberState.SawIntegerDigits;
                                } else if (char.IsWhiteSpace((char) c)) {
                                    state = NumberState.Done;
                                } else {
                                    state = NumberState.Invalid;
                                }
                                break;
                        }
                        break;
                    case NumberState.SawDecimalPoint:
                        type = JsonTokenType.FloatingPoint;
                        if (char.IsDigit((char) c)) {
                            state = NumberState.SawFractionDigits;
                        } else {
                            state = NumberState.Invalid;
                        }
                        break;
                    case NumberState.SawFractionDigits:
                        switch (c) {
                            case 'e':
                            case 'E':
                                state = NumberState.SawExponentLetter;
                                break;
                            case ',':
                            case '}':
                            case ']':
                            case -1:
                                state = NumberState.Done;
                                break;
                            default:
                                if (char.IsDigit((char) c)) {
                                    state = NumberState.SawFractionDigits;
                                } else if (char.IsWhiteSpace((char) c)) {
                                    state = NumberState.Done;
                                } else {
                                    state = NumberState.Invalid;
                                }
                                break;
                        }
                        break;
                    case NumberState.SawExponentLetter:
                        type = JsonTokenType.FloatingPoint;
                        switch (c) {
                            case '+':
                            case '-':
                                state = NumberState.SawExponentSign;
                                break;
                            default:
                                if (char.IsDigit((char) c)) {
                                    state = NumberState.SawExponentDigits;
                                } else {
                                    state = NumberState.Invalid;
                                }
                                break;
                        }
                        break;
                    case NumberState.SawExponentSign:
                        if (char.IsDigit((char) c)) {
                            state = NumberState.SawExponentDigits;
                        } else {
                            state = NumberState.Invalid;
                        }
                        break;
                    case NumberState.SawExponentDigits:
                        switch (c) {
                            case ',':
                            case '}':
                            case ']':
                            case -1:
                                state = NumberState.Done;
                                break;
                            default:
                                if (char.IsDigit((char) c)) {
                                    state = NumberState.SawExponentDigits;
                                } else if (char.IsWhiteSpace((char) c)) {
                                    state = NumberState.Done;
                                } else {
                                    state = NumberState.Invalid;
                                }
                                break;
                        }
                        break;
                }

                switch (state) {
                    case NumberState.Done:
                        return new JsonToken(type, sb.ToString());
                    case NumberState.Invalid:
                        throw new FileFormatException("Invalid JSON number");
                    default:
                        sb.Append((char) c);
                        buffer.Read();
                        break;
                }
            }
        }

        private static JsonToken GetStringToken(
            BsonJsonBuffer buffer
        ) {
            var c = buffer.Read(); // skip opening double quote
            if (c != '"') {
                throw new BsonInternalException("GetStringToken called when next input character was not '\"'");
            }

            var sb = new StringBuilder();
            while (true) {
                c = buffer.Read();
                switch (c) {
                    case '\\':
                        c = buffer.Read();
                        switch (c) {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                var u1 = buffer.Read();
                                var u2 = buffer.Read();
                                var u3 = buffer.Read();
                                var u4 = buffer.Read();
                                if (u4 == -1) {
                                    throw new FileFormatException("End of file in JSON string");
                                }
                                var hex = new string(new char[] { (char) u1, (char) u2, (char) u3, (char) u4 });
                                var n = Convert.ToInt32(hex, 16);
                                sb.Append((char) n);
                                break;
                            case -1:
                                throw new FileFormatException("End of file in JSON string");
                            default:
                                throw new FileFormatException("Invalid escape sequence in JSON string");
                        }
                        break;
                    case '"':
                        return new JsonToken(JsonTokenType.String, sb.ToString());
                    case -1:
                        throw new FileFormatException("End of file in JSON string");
                    default:
                        sb.Append((char) c);
                        break;
                }
            }
        }

        private static JsonToken GetUnquotedStringToken(
            BsonJsonBuffer buffer
        ) {
            var sb = new StringBuilder();
            while (true) {
                var c = buffer.Peek();
                if (char.IsLetterOrDigit((char) c)) {
                    sb.Append((char) c);
                    buffer.Read();
                } else {
                    return new JsonToken(JsonTokenType.UnquotedString, sb.ToString());
                }
            }
        }
        #endregion

        #region nested types
        private enum NumberState {
            Initial,
            SawLeadingMinus,
            SawLeadingZero,
            SawIntegerDigits,
            SawDecimalPoint,
            SawFractionDigits,
            SawExponentLetter,
            SawExponentSign,
            SawExponentDigits,
            Done,
            Invalid
        }
        #endregion
    }
}
