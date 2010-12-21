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

    public static class BsonJsonScanner {
        #region public static methods
        public static JsonToken GetNextToken(
            BsonJsonBuffer buffer
        ) {
            // skip leading whitespace
            var c = buffer.Read();
            while (c != -1 && char.IsWhiteSpace((char) c)) {
                c = buffer.Read();
            }
            if (c == -1) {
                return new JsonToken(JsonTokenType.EndOfFile, "<eof>");
            }

            // leading character determines token type
            switch (c) {
                case '{': return new JsonToken(JsonTokenType.BeginObject, "{");
                case '}': return new JsonToken(JsonTokenType.EndObject, "}");
                case '[': return new JsonToken(JsonTokenType.BeginArray, "[");
                case ']': return new JsonToken(JsonTokenType.EndArray, "]");
                case ':': return new JsonToken(JsonTokenType.Colon, ":");
                case ',': return new JsonToken(JsonTokenType.Comma, ",");
                case '"': return GetStringToken(buffer);
                case '/': return GetRegularExpressionToken(buffer);
                default:
                    if (c == '-' || char.IsDigit((char) c)) {
                        return GetNumberToken(buffer, c);
                    } else if (c == '$' || char.IsLetter((char) c)) {
                        return GetUnquotedStringToken(buffer);
                    } else {
                        buffer.UnRead(c);
                        throw new FileFormatException(FormatMessage("Invalid JSON input", buffer, buffer.Position));
                    }
            }
        }
        #endregion

        #region private methods
        private static string FormatMessage(
            string message,
            BsonJsonBuffer buffer,
            int start
        ) {
            var length = 20;
            string snippet;
            if (buffer.Position + length >= buffer.Length) {
                snippet = buffer.Substring(start);
            } else {
                snippet = buffer.Substring(start, length) + "...";
            }
            return string.Format("{0}: '{1}'", message, snippet);
        }

        private static JsonToken GetNumberToken(
            BsonJsonBuffer buffer,
            int c // first character
        ) {
            // leading digit or '-' has already been read
            var start = buffer.Position - 1;
            NumberState state;
            switch (c) {
                case '-': state = NumberState.SawLeadingMinus; break;
                case '0': state = NumberState.SawLeadingZero; break;
                default: state = NumberState.SawIntegerDigits; break;
            }
            var type = JsonTokenType.Integer; // assume integer until proved otherwise

            while (true) {
                c = buffer.Read();
                switch (state) {
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
                        buffer.UnRead(c);
                        return new JsonToken(type, buffer.Substring(start, buffer.Position - start));
                    case NumberState.Invalid:
                        throw new FileFormatException(FormatMessage("Invalid JSON number", buffer, start));
                }
            }
        }

        private static JsonToken GetRegularExpressionToken(
            BsonJsonBuffer buffer
        ) {
            // opening slash has already been read
            var start = buffer.Position - 1;
            var state = RegularExpressionState.InPattern;
            while (true) {
                var c = buffer.Read();
                switch (state) {
                    case RegularExpressionState.InPattern:
                        switch (c) {
                            case '/': state = RegularExpressionState.InOptions; break;
                            case '\\': state = RegularExpressionState.InEscapeSequence; break;
                            default: state = RegularExpressionState.InPattern; break;
                        }
                        break;
                    case RegularExpressionState.InEscapeSequence:
                        state = RegularExpressionState.InPattern;
                        break;
                    case RegularExpressionState.InOptions:
                        switch (c) {
                            case 'g':
                            case 'i':
                            case 'm':
                                state = RegularExpressionState.InOptions;
                                break;
                            case ',':
                            case '}':
                            case ']':
                            case -1:
                                state = RegularExpressionState.Done;
                                break;
                            default:
                                if (char.IsWhiteSpace((char) c)) {
                                    state = RegularExpressionState.Done;
                                } else {
                                    state = RegularExpressionState.Invalid;
                                }
                                break;
                        }
                        break;
                }

                switch (state) {
                    case RegularExpressionState.Done:
                        buffer.UnRead(c);
                        var count = buffer.Position - start;
                        return new JsonToken(JsonTokenType.RegularExpression, buffer.Substring(start, count));
                    case RegularExpressionState.Invalid:
                        throw new FileFormatException(FormatMessage("Invalid JSON regular expression", buffer, start));
                }
            }
        }

        private static JsonToken GetStringToken(
            BsonJsonBuffer buffer
        ) {
            // opening quote has already been read
            var start = buffer.Position - 1;
            var sb = new StringBuilder();
            while (true) {
                var c = buffer.Read();
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
                                if (u4 != -1) {
                                    var hex = new string(new char[] { (char) u1, (char) u2, (char) u3, (char) u4 });
                                    var n = Convert.ToInt32(hex, 16);
                                    sb.Append((char) n);
                                }
                                break;
                            default:
                                if (c != -1) {
                                    var message = string.Format("Invalid escape sequence in JSON string: '\\{0}'", (char) c);
                                    throw new FileFormatException(message);
                                }
                                break;
                        }
                        break;
                    case '"':
                        return new JsonToken(JsonTokenType.String, sb.ToString());
                    default:
                        if (c != -1) {
                            sb.Append((char) c);
                        }
                        break;
                }
                if (c == -1) {
                    throw new FileFormatException(FormatMessage("End of file in JSON string", buffer, start));
                }
            }
        }

        private static JsonToken GetUnquotedStringToken(
            BsonJsonBuffer buffer
        ) {
            // opening letter or $ has already been read
            var start = buffer.Position - 1;
            while (true) {
                var c = buffer.Read();
                switch (c) {
                    case ':':
                    case ',':
                    case '}':
                    case ']':
                    case -1:
                        buffer.UnRead(c);
                        return new JsonToken(JsonTokenType.UnquotedString, buffer.Substring(start, buffer.Position - start));
                    default:
                        if (c == '$' || char.IsLetterOrDigit((char) c)) {
                            // continue
                        } else if (char.IsWhiteSpace((char) c)) {
                            buffer.UnRead(c);
                            return new JsonToken(JsonTokenType.UnquotedString, buffer.Substring(start, buffer.Position - start));
                        } else {
                            throw new FileFormatException(FormatMessage("Invalid JSON unquoted string", buffer, start));
                        }
                        break;
                }
            }
        }
        #endregion

        #region nested types
        private enum NumberState {
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

        private enum RegularExpressionState {
            InPattern,
            InEscapeSequence,
            InOptions,
            Done,
            Invalid
        }
        #endregion
    }
}
