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
using System.Linq;
using System.Text;

namespace MongoDB.Bson {
    [Serializable]
    public enum BsonType {
        EndOfDocument = 0x00, // no values of this type exist, it marks the end of a document
        Double = 0x01,
        String = 0x02,
        Document = 0x03,
        Array = 0x04,
        Binary = 0x05,
        ObjectId = 0x07,
        Boolean = 0x08,
        DateTime = 0x09,
        Null = 0x0a,
        RegularExpression = 0x0b,
        JavaScript = 0x0d,
        Symbol = 0x0e,
        JavaScriptWithScope = 0x0f,
        Int32 = 0x10,
        Timestamp = 0x11,
        Int64 = 0x12,
        MinKey = 0xff,
        MaxKey = 0x7f
    }
}
