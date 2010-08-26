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

namespace MongoDB.MongoDBClient.Internal {
    internal class MongoGetMoreMessage : MongoRequestMessage {
        #region private fields
        private int numberToReturn;
        private long cursorId;
        #endregion

        #region constructors
        internal MongoGetMoreMessage(
            MongoCollection collection,
            int numberToReturn,
            long cursorId
        )
            : base(MessageOpcode.GetMore, collection) {
            this.collection = collection;
            this.numberToReturn = numberToReturn;
            this.cursorId = cursorId;
        }
        #endregion

        #region protected methods
        protected override void WriteBodyTo(
            BinaryWriter binaryWriter
        ) {
            binaryWriter.Write((int) 0); // reserved
            WriteCStringTo(binaryWriter, collection.FullName); // fullCollectionName
            binaryWriter.Write(numberToReturn);
            binaryWriter.Write(cursorId);
        }
        #endregion
    }
}
