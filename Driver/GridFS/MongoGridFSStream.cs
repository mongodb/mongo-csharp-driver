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
    public class MongoGridFSStream : Stream {
        #region private fields
        private MongoGridFSFileInfo fileInfo;
        private FileMode mode;
        private FileAccess access;
        #endregion

        #region constructors
        public MongoGridFSStream(
            MongoGridFSFileInfo fileInfo,
            FileMode mode
        )
            : this(fileInfo, mode, FileAccess.ReadWrite) {
        }

        public MongoGridFSStream(
            MongoGridFSFileInfo fileInfo,
            FileMode mode,
            FileAccess access
        ) {
            this.fileInfo = fileInfo;
            this.mode = mode;
            this.access = access;

            var exists = fileInfo.Exists;
            string message;
            switch (mode) {
                case FileMode.Append:
                    if (exists) {
                        Append();
                    } else {
                        Create();
                    }
                    break;
                case FileMode.Create:
                    if (exists) {
                        Truncate();
                    } else {
                        Create();
                    }
                    break;
                case FileMode.CreateNew:
                    if (exists) {
                        message = string.Format("File already exists: {0}", fileInfo.Name);
                        throw new IOException(message);
                    } else {
                        Create();
                    }
                    break;
                case FileMode.Open:
                    if (exists) {
                        Open();
                    } else {
                        message = string.Format("File not found: {0}", fileInfo.Name);
                        throw new FileNotFoundException(message);
                    }
                    break;
                case FileMode.OpenOrCreate:
                    if (exists) {
                        Open();
                    } else {
                        Create();
                    }
                    break;
                case FileMode.Truncate:
                    if (exists) {
                        Truncate();
                    } else {
                        message = string.Format("File not found: {0}", fileInfo.Name);
                        throw new FileNotFoundException(message);
                    }
                    break;
                default:
                    message = string.Format("Invalid FileMode: {0}", fileInfo.Name);
                    throw new ArgumentException(message, "mode");
            }
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

        public override void WriteByte(
            byte value
        ) {
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

        #region private methods
        private void Append() {
            throw new NotImplementedException();
        }

        private void Create() {
            throw new NotImplementedException();
        }

        private void Open() {
            throw new NotImplementedException();
        }

        private void Truncate() {
            throw new NotImplementedException();
        }
        #endregion
    }
}
