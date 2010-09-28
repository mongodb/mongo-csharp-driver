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
    // the state of a BsonReader
    public enum BsonReadState {
        Initial, // ReadStartDocument should be called next
        Type, // ReadType should be called next
        Name, // ReadName should be called next
        Value, // Read<Type> should be called next
        EndOfDocument, // ReadEndDocument should be called next
        EndOfEmbeddedDocument, // ReadEndEmbeddedDocument should be called next
        EndOfArray, // ReadEndArray should be called next
        EndOfScopeDocument, // ReadEndOfJavaScriptWithScope should be called next
        Done, // an entire document has been read
        Error,
        Closed
    }
}
