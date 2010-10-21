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

namespace MongoDB.Driver.GridFS {
    // when implemented this class will allow the client to read and write GridFS files as if they were regular streams
    public class MongoGridFSStream : Stream {
        #region private fields
        #endregion

        #region constructors
        public MongoGridFSStream() {
        }
        #endregion

        #region public properties
        public override bool CanRead {
            get { throw new NotImplementedException(); }
        }

        public override bool CanSeek {
            get { throw new NotImplementedException(); }
        }

        public override bool CanWrite {
            get { throw new NotImplementedException(); }
        }

        public override long Length {
            get { throw new NotImplementedException(); }
        }

        public override long Position {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }
        #endregion

        #region public methods
        public override void Close() {
            throw new NotImplementedException();
        }

        public override void Flush() {
            throw new NotImplementedException();
        }

        public override int Read(
            byte[] buffer,
            int offset,
            int count
        ) {
            throw new NotImplementedException();
        }

        public override int ReadByte() {
            return base.ReadByte();
        }

        public override long Seek(
            long offset,
            SeekOrigin origin
        ) {
            throw new NotImplementedException();
        }

        public override void SetLength(
            long value
        ) {
            throw new NotImplementedException();
        }

        public override void Write(
            byte[] buffer,
            int offset,
            int count
        ) {
            throw new NotImplementedException();
        }

        public override void WriteByte(byte value) {
            base.WriteByte(value);
        }
        #endregion

        #region protected methods
        protected override void Dispose(
            bool disposing
        ) {
            base.Dispose(disposing);
        }
        #endregion
    }
}
