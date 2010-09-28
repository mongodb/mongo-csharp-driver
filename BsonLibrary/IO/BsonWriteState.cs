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

namespace MongoDB.BsonLibrary.IO {
    public enum BsonWriteState {
        Initial, // must call StartDocument next
        StartDocument, // must call StartDocument next
        Done, // can call StartDocument if writing a second document using the same writer
        Closed, // can't use this writer anymore
        Error, // can't use this writer anymore

        // NOTE: all the Document states start with 0x80 to facilitate checking for them as a group
        Document = 0x80,
        EmbeddedDocument = Document | 0x01,
        Array = Document | 0x02,
        JavaScriptWithScope = Document | 0x03, // used internally (callers will only see ScopeDocument)
        ScopeDocument = Document | 0x04
    }
}
