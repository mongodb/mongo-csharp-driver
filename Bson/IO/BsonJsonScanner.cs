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

namespace MongoDB.Bson.IO {
    public enum JsonTokenType {
        Invalid,
        BeginArray,
        BeginObject,
        EndArray,
        EndObject,
        NameSeparator,
        ValueSeparator,
        Boolean,
        Null,
        Integer,
        FloatingPoint,
        String,
        UnquotedString,
        EndOfFile
    }

    public class JsonToken {
        public JsonTokenType Type;
        public string Value;
    }

    public static class BsonJsonScanner {
        #region public static methods
        public static JsonToken GetNextToken(
            TextReader reader
        ) {
            // skip leading whitespace
            var c = reader.Read();
            while (c != -1 && char.IsWhiteSpace((char) c)) {
                c = reader.Read();
            }

            // check for end of file
            if (c == -1) {
                return new JsonToken { Type = JsonTokenType.EndOfFile };
            }

            // check for single character tokens
            switch (c) {
                case '{': return new JsonToken { Type = JsonTokenType.BeginObject };
                case '}': return new JsonToken { Type = JsonTokenType.EndObject };
                case '[': return new JsonToken { Type = JsonTokenType.BeginArray };
                case ']': return new JsonToken { Type = JsonTokenType.EndArray };
                case ':': return new JsonToken { Type = JsonTokenType.NameSeparator };
                case ',': return new JsonToken { Type = JsonTokenType.ValueSeparator };
            }

            // scan strings
            if (c == '"') {
                return GetStringToken(reader, c);
            }

            // scan numbers
            if (c == '-' || char.IsDigit((char) c)) {
                return GetNumberToken(reader, c);
            }

            // scan unquoted strings (not strictly JSON but commonly supported for names)
            if (char.IsLetter((char) c)) {
                return GetUnquotedStringToken(reader, c); // also checks for true, false and null
            }

            throw new FileFormatException("Invalid JSON input");
        }
        #endregion

        #region private methods
        private static JsonToken GetNumberToken(
            TextReader reader,
            int c
        ) {
            var state = NumberState.Initial;
            var type = JsonTokenType.Integer; // assume integer until proved otherwise
            var sb = new StringBuilder();
            while (true) {
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
                                    state = NumberState.IntegerDigits;
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
                                    state = NumberState.IntegerDigits;
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
                    case NumberState.IntegerDigits:
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
                                    state = NumberState.IntegerDigits;
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
                            state = NumberState.FractionDigits;
                        } else {
                            state = NumberState.Invalid;
                        }
                        break;
                    case NumberState.FractionDigits:
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
                                    state = NumberState.FractionDigits;
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
                                    state = NumberState.ExponentDigits;
                                } else {
                                    state = NumberState.Invalid;
                                }
                                break;
                        }
                        break;
                    case NumberState.SawExponentSign:
                        if (char.IsDigit((char) c)) {
                            state = NumberState.ExponentDigits;
                        } else {
                            state = NumberState.Invalid;
                        }
                        break;
                    case NumberState.ExponentDigits:
                        switch (c) {
                            case ',':
                            case '}':
                            case ']':
                            case -1:
                                state = NumberState.Done;
                                break;
                            default:
                                if (char.IsDigit((char) c)) {
                                    state = NumberState.ExponentDigits;
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
                        // TODO: return c to input buffer
                        return new JsonToken { Type = type, Value = sb.ToString() };
                    case NumberState.Invalid:
                        throw new FileFormatException("Invalid JSON number");
                    default:
                        sb.Append((char) c);
                        c = reader.Read();
                        break;
                }
            }
        }

        private static JsonToken GetStringToken(
            TextReader reader,
            int c
        ) {
            var sb = new StringBuilder();
            while ((c = reader.Read()) != '"') {
                if (c == -1) {
                    throw new FileFormatException("End of file in JSON string");
                }
                if (c == '\\') {
                    c = reader.Read();
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
                            var u1 = reader.Read();
                            var u2 = reader.Read();
                            var u3 = reader.Read();
                            var u4 = reader.Read();
                            if (u4 == -1) {
                                throw new FileFormatException("End of file in JSON string");
                            }
                            var hex = new string(new char[] { (char) u1, (char) u2, (char) u3, (char) u4 });
                            var n = Convert.ToInt32(hex, 16);
                            sb.Append((char) n);
                            break;
                        default:
                            throw new FileFormatException("Invalid escape sequence in JSON string");
                    }
                } else {
                    sb.Append((char) c);
                }
            }
            return new JsonToken { Type = JsonTokenType.String, Value = sb.ToString() };
        }

        private static JsonToken GetUnquotedStringToken(
            TextReader reader,
            int c
        ) {
            var sb = new StringBuilder();
            do {
                sb.Append((char) c);
                c = reader.Read();
            } while (char.IsLetterOrDigit((char) c));
            var s = sb.ToString();
            switch (s) {
                case "true":
                case "false":
                    return new JsonToken { Type = JsonTokenType.Boolean, Value = s };
                case "null":
                    return new JsonToken { Type = JsonTokenType.Null };
                default:
                    return new JsonToken { Type = JsonTokenType.UnquotedString, Value = sb.ToString() };
            }
        }
        #endregion

        #region nested types
        private enum NumberState {
            Initial,
            SawLeadingMinus,
            SawLeadingZero,
            IntegerDigits,
            SawDecimalPoint,
            FractionDigits,
            SawExponentLetter,
            SawExponentSign,
            ExponentDigits,
            Done,
            Invalid
        }
        #endregion
    }
}
