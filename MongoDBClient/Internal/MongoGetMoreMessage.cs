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
    internal class MongoGetMoreMessage : MongoMessage {
        #region private fields
        private MongoCollection collection;
        private int numberToReturn;
        private long cursorID;
        #endregion

        #region constructors
        internal MongoGetMoreMessage(
            MongoCollection collection,
            int numberToReturn,
            long cursorID
        )
            : base(RequestOpCode.GetMore) {
            this.collection = collection;
            this.numberToReturn = numberToReturn;
            this.cursorID = cursorID;
        }
        #endregion

        #region protected methods
        protected override void WriteBodyTo(
            BinaryWriter writer
        ) {
            writer.Write((int) 0); // reserved
            WriteCString(writer, collection.FullName); // fullCollectionName
            writer.Write(numberToReturn);
            writer.Write(cursorID);
        }
        #endregion
    }
}
