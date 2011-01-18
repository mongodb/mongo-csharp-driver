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

namespace MongoDB.Driver {
    [Serializable]
    public class SafeMode {
        #region private static fields
        private static SafeMode @false = new SafeMode(false);
        private static SafeMode fsyncTrue = new SafeMode(true, true);
        private static SafeMode @true = new SafeMode(true, false);
        private static SafeMode w2 = new SafeMode(true, false, 2);
        private static SafeMode w3 = new SafeMode(true, false, 3);
        private static SafeMode w4 = new SafeMode(true, false, 4);
        #endregion

        #region private fields
        private bool enabled;
        private bool fsync;
        private int w;
        private TimeSpan wtimeout;
        #endregion

        #region constructors
        public SafeMode(
            bool enabled
        )
            : this(enabled, false) {
        }

        public SafeMode(
            bool enabled,
            bool fsync
        )
            : this(enabled, fsync, 0) {
        }

        public SafeMode(
            bool enabled,
            bool fsync,
            int w
        )
            : this(enabled, fsync, w, TimeSpan.Zero) {
        }

        public SafeMode(
            bool enabled,
            bool fsync,
            int w,
            TimeSpan wtimeout
        ) {
            if (fsync && !enabled) {
                throw new ArgumentException("fsync cannot be true when SafeMode is not enabled");
            }
            if (w != 0 && !enabled) {
                throw new ArgumentException("w cannot be non-zero when SafeMode is not enabled");
            }
            if (wtimeout != TimeSpan.Zero && w == 0) {
                throw new ArgumentException("wtimeout cannot be non-zero when w is zero");
            }

            this.enabled = enabled;
            this.fsync = fsync;
            this.w = w;
            this.wtimeout = wtimeout;
        }

        public SafeMode(
            int w
        )
            : this(true, false, w) {
        }

        public SafeMode(
            int w,
            TimeSpan wtimeout
        )
            : this(true, false, w, wtimeout) {
        }
        #endregion

        #region public static properties
        public static SafeMode False {
            get { return @false; }
        }

        public static SafeMode FSyncTrue {
            get { return fsyncTrue; }
        }

        public static SafeMode True {
            get { return @true; }
        }

        public static SafeMode W2 {
            get { return w2; }
        }

        public static SafeMode W3 {
            get { return w3; }
        }

        public static SafeMode W4 {
            get { return w4; }
        }
        #endregion

        #region public properties
        public bool Enabled {
            get { return enabled; }
        }

        public bool FSync {
            get { return fsync; }
        }

        public int W {
            get { return w; }
        }

        public TimeSpan WTimeout {
            get { return wtimeout; }
        }
        #endregion

        #region public operators
        public static bool operator ==(
            SafeMode lhs,
            SafeMode rhs
        ) {
            return object.Equals(lhs, rhs);
        }

        public static bool operator !=(
            SafeMode lhs,
            SafeMode rhs
        ) {
            return !(lhs == rhs);
        }
        #endregion

        #region public static methods
        public static SafeMode Create(
            bool enabled
        ) {
            return Create(enabled, false);
        }

        public static SafeMode Create(
            bool enabled,
            bool fsync
        ) {
            return Create(enabled, fsync, 0);
        }

        public static SafeMode Create(
            bool enabled,
            bool fsync,
            int w
        ) {
            return Create(enabled, fsync, w, TimeSpan.Zero);
        }

        public static SafeMode Create(
            bool enabled,
            bool fsync,
            int w,
            TimeSpan wtimeout
        ) {
            if (!fsync && wtimeout == TimeSpan.Zero) {
                if (enabled) {
                    switch (w) {
                        case 2: return w2;
                        case 3: return w3;
                        case 4: return w4;
                        default: return new SafeMode(true, false, w);
                    }
                } else if (w == 0) {
                    return @false;
                }
            }
            return new SafeMode(enabled, fsync, w, wtimeout);
        }

        public static SafeMode Create(
            int w
        ) {
            return Create(w, TimeSpan.Zero);
        }

        public static SafeMode Create(
            int w,
            TimeSpan wtimeout
        ) {
            return Create(true, false, w, wtimeout);
        }
        #endregion

        #region public methods
        public override bool Equals(
            object obj
        ) {
            return Equals(obj as SafeMode); // works even if obj is null
        }

        public bool Equals(
            SafeMode rhs
        ) {
            if (rhs == null) { return false; }
            return this.enabled == rhs.enabled && this.fsync == rhs.fsync && this.w == rhs.w && this.wtimeout == rhs.wtimeout;
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + enabled.GetHashCode();
            hash = 37 * hash + fsync.GetHashCode();
            hash = 37 * hash + w.GetHashCode();
            hash = 37 * hash + wtimeout.GetHashCode();
            return hash;
        }

        public override string ToString() {
            if (enabled) {
                var sb = new StringBuilder("safe=true");
                if (fsync) {
                    sb.Append(",fsync=true");
                }
                if (w != 0) {
                    sb.AppendFormat(",w={0}", w);
                    if (wtimeout != TimeSpan.Zero) {
                        sb.AppendFormat(",wtimeout={0}", wtimeout);
                    }
                }
                return sb.ToString();
            } else {
                return "safe=false";
            }
        }
        #endregion
    }
}
