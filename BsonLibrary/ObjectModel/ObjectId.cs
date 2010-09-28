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
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace MongoDB.BsonLibrary {
    public struct ObjectId : IComparable<ObjectId>, IEquatable<ObjectId> {
        #region private static fields
        private static long machinePid;
        private static int increment; // high byte will be masked out when generating new ObjectId
        #endregion

        #region private fields
        // store as an int and a long because they are structs (a byte[] would be allocated on the heap)
        private int timestamp;
        private long machinePidIncrement;
        #endregion

        #region static constructor
        static ObjectId() {
            int machine = GetMachineHash();
            int pid = Process.GetCurrentProcess().Id;
            machinePid = ((long) machine << 40) + ((long) pid << 24);
            increment = (new Random()).Next();
        }
        #endregion

        #region constructors
        public ObjectId(
            byte[] bytes
        ) {
            Unpack(bytes, out timestamp, out machinePidIncrement);
        }

        public ObjectId(
            int timestamp,
            long machinePidIncrement
        ) {
            this.timestamp = timestamp;
            this.machinePidIncrement = machinePidIncrement;
        }

        public ObjectId(
            string value
        ) {
            Unpack(BsonUtils.ParseHexString(value), out timestamp, out machinePidIncrement);
        }
        #endregion

        #region public properties
        public int Timestamp {
            get { return timestamp; }
        }

        public long MachinePidIncrement {
            get { return machinePidIncrement; }
        }

        public int Machine {
            get { return (int) (machinePidIncrement >> 40); }
        }

        public int Pid {
            get { return (int) (machinePidIncrement >> 24) & 0x0000ffff; }
        }

        public int Increment {
            get { return (int) machinePidIncrement & 0x00ffffff; }
        }

        // a more or less accurate creation time derived from Timestamp
        public DateTime CreationTime {
            get { return Bson.UnixEpoch.AddSeconds(timestamp); }
        }
        #endregion

        #region public operators
        public static bool operator <(
            ObjectId lhs,
            ObjectId rhs
        ) {
            return lhs.CompareTo(rhs) < 0;
        }

        public static bool operator <=(
            ObjectId lhs,
            ObjectId rhs
        ) {
            return lhs.CompareTo(rhs) <= 0;
        }

        public static bool operator ==(
            ObjectId lhs,
            ObjectId rhs
        ) {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(
            ObjectId lhs,
            ObjectId rhs
        ) {
            return !lhs.Equals(rhs);
        }

        public static bool operator >=(
            ObjectId lhs,
            ObjectId rhs
        ) {
            return lhs.CompareTo(rhs) >= 0;
        }

        public static bool operator >(
            ObjectId lhs,
            ObjectId rhs
        ) {
            return lhs.CompareTo(rhs) > 0;
        }
        #endregion

        #region public static methods
        public static ObjectId GenerateNewId() {
            int timestamp = GetCurrentTimestamp();
            int increment = Interlocked.Increment(ref ObjectId.increment) & 0x00ffffff; // only use low order 3 bytes
            return new ObjectId(timestamp, machinePid + increment);
        }

        public static byte[] Pack(
            int timestamp,
            long machinePidIncrement
        ) {
            byte[] bytes = new byte[12];
            bytes[0] = (byte) (timestamp >> 24);
            bytes[1] = (byte) (timestamp >> 16);
            bytes[2] = (byte) (timestamp >> 8);
            bytes[3] = (byte) (timestamp);
            bytes[4] = (byte) (machinePidIncrement >> 56);
            bytes[5] = (byte) (machinePidIncrement >> 48);
            bytes[6] = (byte) (machinePidIncrement >> 40);
            bytes[7] = (byte) (machinePidIncrement >> 32);
            bytes[8] = (byte) (machinePidIncrement >> 24);
            bytes[9] = (byte) (machinePidIncrement >> 16);
            bytes[10] = (byte) (machinePidIncrement >> 8);
            bytes[11] = (byte) (machinePidIncrement);
            return bytes;
        }

        public static ObjectId Parse(
            string s
        ) {
            ObjectId objectId;
            if (TryParse(s, out objectId)) {
                return objectId;
            } else {
                throw new FormatException("Argument is not a valid 24 digit hex string");
            }
        }

        public static bool TryParse(
            string s,
            out ObjectId objectId
        ) {
            if (s == null) {
                throw new ArgumentNullException("s");
            }

            if (s.Length == 24) {
                byte[] bytes;
                if (BsonUtils.TryParseHexString(s, out bytes)) {
                    objectId = new ObjectId(bytes);
                    return true;
                }
            }

            objectId = default(ObjectId);
            return false;
        }

        public static void Unpack(
            byte[] bytes,
            out int timestamp,
            out long machinePidIncrement
        ) {
            if (bytes.Length != 12) {
                throw new ArgumentOutOfRangeException("Byte array must be 12 bytes long");
            }
            timestamp = (bytes[0] << 24) + (bytes[1] << 16) + (bytes[2] << 8) + bytes[3];
            int machine = (bytes[4] << 16) + (bytes[5] << 8) + bytes[6];
            int pid = (bytes[7] << 8) + bytes[8];
            int increment = (bytes[9] << 16) + (bytes[10] << 8) + bytes[11];
            machinePidIncrement = ((long) machine << 40) + ((long) pid << 24) + increment;
        }
        #endregion

        #region private static methods
        private static int GetMachineHash() {
            var hostName = Environment.MachineName; // use instead of Dns.HostName so it will work offline
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(hostName));
            return (hash[0] << 16) + (hash[1] << 8) + hash[2]; // use first 3 bytes of hash
        }

        private static int GetCurrentTimestamp() {
            DateTime now = DateTime.UtcNow;
            return (int) Math.Floor((now - Bson.UnixEpoch).TotalSeconds);
        }
        #endregion

        #region public methods
        public int CompareTo(
            ObjectId other
        ) {
            int r = timestamp.CompareTo(other.timestamp);
            if (r != 0) { return r; }
            return machinePidIncrement.CompareTo(other.machinePidIncrement);
        }

        public bool Equals(
            ObjectId rhs
        ) {
            return this.timestamp == rhs.timestamp && this.machinePidIncrement == rhs.machinePidIncrement;
        }

        public override bool Equals(
            object obj
        ) {
            if (obj is ObjectId) {
                return Equals((ObjectId) obj);
            } else {
                return false;
            }
        }

        public override int GetHashCode() {
            int hash = 17;
            hash = 37 * hash + timestamp.GetHashCode();
            hash = 37 * hash + machinePidIncrement.GetHashCode();
            return hash;
        }

        public byte[] ToByteArray() {
            return Pack(timestamp, machinePidIncrement);
        }

        public override string ToString() {
            return BsonUtils.ToHexString(Pack(timestamp, machinePidIncrement));
        }
        #endregion
    }
}
