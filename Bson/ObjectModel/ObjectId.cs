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
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace MongoDB.Bson {
    [Serializable]
    public struct ObjectId : IComparable<ObjectId>, IEquatable<ObjectId> {
        #region private static fields
        private static ObjectId emptyInstance = default(ObjectId);
        private static int staticMachine;
        private static short staticPid;
        private static int staticIncrement; // high byte will be masked out when generating new ObjectId
        #endregion

        #region private fields
        // we're using 14 bytes instead of 12 to hold the ObjectId in memory but unlike a byte[] there is no additional object on the heap
        // the extra two bytes are not visible to anyone outside of this class and they buy us considerable simplification
        // an additional advantage of this representation is that it will serialize to JSON without any 64 bit overflow problems
        private int timestamp;
        private int machine;
        private short pid;
        private int increment;
        #endregion

        #region static constructor
        static ObjectId() {
            staticMachine = GetMachineHash();
            staticPid = (short) Process.GetCurrentProcess().Id; // use low order two bytes only
            staticIncrement = (new Random()).Next();
        }
        #endregion

        #region constructors
        public ObjectId(
            byte[] bytes
        ) {
            Unpack(bytes, out timestamp, out machine, out pid, out increment);
        }

        public ObjectId(
            int timestamp,
            int machine,
            short pid,
            int increment
        ) {
            this.timestamp = timestamp;
            this.machine = machine;
            this.pid = pid;
            this.increment = increment;
        }

        public ObjectId(
            string value
        ) {
            Unpack(BsonUtils.ParseHexString(value), out timestamp, out machine, out pid, out increment);
        }
        #endregion

        #region public static properties
        public static ObjectId Empty {
            get { return emptyInstance; }
        }
        #endregion

        #region public properties
        public int Timestamp {
            get { return timestamp; }
        }

        public int Machine {
            get { return machine; }
        }

        public short Pid {
            get { return pid; }
        }

        public int Increment {
            get { return increment; }
        }

        // a more or less accurate creation time derived from Timestamp
        public DateTime CreationTime {
            get { return BsonConstants.UnixEpoch.AddSeconds(timestamp); }
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
            int increment = Interlocked.Increment(ref ObjectId.staticIncrement) & 0x00ffffff; // only use low order 3 bytes
            return new ObjectId(timestamp, staticMachine, staticPid, increment);
        }

        public static byte[] Pack(
            int timestamp,
            int machine,
            short pid,
            int increment
        ) {
            byte[] bytes = new byte[12];
            bytes[0] = (byte) (timestamp >> 24);
            bytes[1] = (byte) (timestamp >> 16);
            bytes[2] = (byte) (timestamp >> 8);
            bytes[3] = (byte) (timestamp);
            bytes[4] = (byte) (machine >> 16);
            bytes[5] = (byte) (machine >> 8);
            bytes[6] = (byte) (machine);
            bytes[7] = (byte) (pid >> 8);
            bytes[8] = (byte) (pid);
            bytes[9] = (byte) (increment >> 16);
            bytes[10] = (byte) (increment >> 8);
            bytes[11] = (byte) (increment);
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
            out int machine,
            out short pid,
            out int increment
        ) {
            if (bytes.Length != 12) {
                throw new ArgumentOutOfRangeException("Byte array must be 12 bytes long");
            }
            timestamp = (bytes[0] << 24) + (bytes[1] << 16) + (bytes[2] << 8) + bytes[3];
            machine = (bytes[4] << 16) + (bytes[5] << 8) + bytes[6];
            pid = (short) ((bytes[7] << 8) + bytes[8]);
            increment = (bytes[9] << 16) + (bytes[10] << 8) + bytes[11];
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
            return (int) Math.Floor((now - BsonConstants.UnixEpoch).TotalSeconds);
        }
        #endregion

        #region public methods
        public int CompareTo(
            ObjectId other
        ) {
            int r = timestamp.CompareTo(other.timestamp);
            if (r != 0) { return r; }
            r = machine.CompareTo(other.machine);
            if (r != 0) { return r; }
            r = pid.CompareTo(other.pid);
            if (r != 0) { return r; }
            return increment.CompareTo(other.increment);
        }

        public bool Equals(
            ObjectId rhs
        ) {
            return
                this.timestamp == rhs.timestamp &&
                this.machine == rhs.machine &&
                this.pid == rhs.pid &&
                this.increment == rhs.increment;
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
            hash = 37 * hash + machine.GetHashCode();
            hash = 37 * hash + pid.GetHashCode();
            hash = 37 * hash + increment.GetHashCode();
            return hash;
        }

        public byte[] ToByteArray() {
            return Pack(timestamp, machine, pid, increment);
        }

        public override string ToString() {
            return BsonUtils.ToHexString(Pack(timestamp, machine, pid, increment));
        }
        #endregion
    }
}
