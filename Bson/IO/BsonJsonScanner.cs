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
            var c = buffer.Peek();
            while (c != -1 && char.IsWhiteSpace((char) c)) {
                buffer.Read(); // ignore whitespace
                c = buffer.Peek();
            }
            var start = buffer.Position;

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

            // scan regular expressions
            if (c == '/') {
                return GetRegularExpressionToken(buffer);
            }

            // scan numbers
            if (c == '-' || char.IsDigit((char) c)) {
                return GetNumberToken(buffer);
            }

            // true, false and null are returned as unquoted strings and detected by the parser
            if (char.IsLetter((char) c)) {
                return GetUnquotedStringToken(buffer);
            }

            throw new FileFormatException(FormatMessage("Invalid JSON input", buffer, start));
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
            if (buffer.Position + length > buffer.Length) {
                snippet = buffer.Substring(start);
            } else {
                snippet = buffer.Substring(start, length) + "...";
            }
            return string.Format("{0}: '{1}'", message, snippet);
        }

        private static JsonToken GetNumberToken(
            BsonJsonBuffer buffer
        ) {
            var start = buffer.Position;
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
                        throw new FileFormatException(FormatMessage("Invalid JSON number", buffer, start));
                    default:
                        sb.Append((char) c);
                        buffer.Read();
                        break;
                }
            }
        }

        private static JsonToken GetRegularExpressionToken(
            BsonJsonBuffer buffer
        ) {
            var start = buffer.Position;
            var state = RegularExpressionState.Initial;
            while (true) {
                var c = buffer.Read();
                switch (state) {
                    case RegularExpressionState.Initial:
                        switch (c) {
                            case '/': state = RegularExpressionState.InPattern; break;
                            default: state = RegularExpressionState.Invalid; break;
                        }
                        break;
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
                                buffer.Position -= 1;
                                state = RegularExpressionState.Done;
                                break;
                            default:
                                if (c == -1 || char.IsWhiteSpace((char) c)) {
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
            var start = buffer.Position;
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
                                    throw new FileFormatException(FormatMessage("End of file in JSON string", buffer, start));
                                }
                                var hex = new string(new char[] { (char) u1, (char) u2, (char) u3, (char) u4 });
                                var n = Convert.ToInt32(hex, 16);
                                sb.Append((char) n);
                                break;
                            case -1:
                                throw new FileFormatException(FormatMessage("End of file in JSON string", buffer, start));
                            default:
                                var message = string.Format("Invalid escape sequence in JSON string: '\\{0}'", (char) c);
                                throw new FileFormatException(message);
                        }
                        break;
                    case '"':
                        return new JsonToken(JsonTokenType.String, sb.ToString());
                    case -1:
                        throw new FileFormatException(FormatMessage("End of file in JSON string", buffer, start));
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

        private enum RegularExpressionState {
            Initial,
            InPattern,
            InEscapeSequence,
            InOptions,
            Done,
            Invalid
        }
        #endregion
    }
}
