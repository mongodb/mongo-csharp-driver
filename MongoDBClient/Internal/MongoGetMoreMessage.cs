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
        private string fullCollectionName;
        private int numberToReturn;
        private long cursorID;
        #endregion

        #region constructors
        internal MongoGetMoreMessage()
            : base(RequestOpCode.GetMore) {
        }
        #endregion

        #region public properties
        public string FullCollectionName {
            get { return fullCollectionName; }
            set { fullCollectionName = value; }
        }

        public int NumberToReturn {
            get { return numberToReturn; }
            set { numberToReturn = value; }
        }

        public long CursorID {
            get { return cursorID; }
            set { cursorID = value; }
        }
        #endregion

        #region protected methods
        protected override void WriteBodyTo(
            BinaryWriter writer
        ) {
            throw new NotImplementedException();
        }
        #endregion
    }
}
