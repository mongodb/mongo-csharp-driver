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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the different safe modes that can be used.
    /// </summary>
    [Serializable]
    public class SafeMode : IEquatable<SafeMode>
    {
        // private static fields
        private static SafeMode @false = new SafeMode(false);
        private static SafeMode fsyncTrue = new SafeMode(true, true);
        private static SafeMode @true = new SafeMode(true, false);
        private static SafeMode w2 = new SafeMode(true, false, 2);
        private static SafeMode w3 = new SafeMode(true, false, 3);
        private static SafeMode w4 = new SafeMode(true, false, 4);

        // private fields
        private bool enabled;
        private bool fsync;
        private bool j;
        private int w;
        private string wmode;
        private TimeSpan wtimeout;
        private bool isFrozen;
        private int frozenHashCode;

        // constructors
        /// <summary>
        /// Creates a new instance of the SafeMode class.
        /// </summary>
        /// <param name="enabled">Whether safe mode is enabled.</param>
        public SafeMode(bool enabled)
            : this(enabled, false)
        {
        }

        /// <summary>
        /// Creates a new instance of the SafeMode class.
        /// </summary>
        /// <param name="enabled">Whether safe mode is enabled.</param>
        /// <param name="fsync">Whether the server should call fsync after each operation.</param>
        public SafeMode(bool enabled, bool fsync)
            : this(enabled, fsync, 0)
        {
        }

        /// <summary>
        /// Creates a new instance of the SafeMode class.
        /// </summary>
        /// <param name="enabled">Whether safe mode is enabled.</param>
        /// <param name="fsync">Whether the server should call fsync after each operation.</param>
        /// <param name="w">The number of write replications that should be completed before server returns.</param>
        public SafeMode(bool enabled, bool fsync, int w)
            : this(enabled, fsync, w, TimeSpan.Zero)
        {
        }

        /// <summary>
        /// Creates a new instance of the SafeMode class.
        /// </summary>
        /// <param name="enabled">Whether safe mode is enabled.</param>
        /// <param name="fsync">Whether the server should call fsync after each operation.</param>
        /// <param name="w">The number of write replications that should be completed before server returns.</param>
        /// <param name="wtimeout">The timeout for each operation.</param>
        public SafeMode(bool enabled, bool fsync, int w, TimeSpan wtimeout)
        {
            if (fsync && !enabled)
            {
                throw new ArgumentException("fsync cannot be true when SafeMode is not enabled.");
            }
            if (w != 0 && !enabled)
            {
                throw new ArgumentException("w cannot be non-zero when SafeMode is not enabled.");
            }
            if (wtimeout != TimeSpan.Zero && w == 0)
            {
                throw new ArgumentException("wtimeout cannot be non-zero when w is zero.");
            }

            this.enabled = enabled;
            this.fsync = fsync;
            this.w = w;
            this.wtimeout = wtimeout;
        }

        /// <summary>
        /// Creates a new instance of the SafeMode class.
        /// </summary>
        /// <param name="w">The number of write replications that should be completed before server returns.</param>
        public SafeMode(int w)
            : this(true, false, w)
        {
        }

        /// <summary>
        ///Creates a new instance of the SafeMode class.
        /// </summary>
        /// <param name="w">The number of write replications that should be completed before server returns.</param>
        /// <param name="wtimeout">The timeout for each operation.</param>
        public SafeMode(int w, TimeSpan wtimeout)
            : this(true, false, w, wtimeout)
        {
        }

        /// <summary>
        ///Creates a new instance of the SafeMode class.
        /// </summary>
        /// <param name="other">Another SafeMode to initialize this one from.</param>
        public SafeMode(SafeMode other)
            : this(false)
        {
            if (other != null)
            {
                this.enabled = other.enabled;
                this.fsync = other.fsync;
                this.j = other.j;
                this.w = other.w;
                this.wmode = other.wmode;
                this.wtimeout = other.wtimeout;
            }
        }

        // public static properties
        /// <summary>
        /// Gets an instance of SafeMode with safe mode off.
        /// </summary>
        public static SafeMode False
        {
            get { return @false; }
        }

        /// <summary>
        /// Gets an instance of SafeMode with fsync=true.
        /// </summary>
        public static SafeMode FSyncTrue
        {
            get { return fsyncTrue; }
        }

        /// <summary>
        /// Gets an instance of SafeMode with safe mode on.
        /// </summary>
        public static SafeMode True
        {
            get { return @true; }
        }

        /// <summary>
        /// Gets an instance of SafeMode with safe mode on and w=2.
        /// </summary>
        public static SafeMode W2
        {
            get { return w2; }
        }

        /// <summary>
        /// Gets an instance of SafeMode with safe mode on and w=3.
        /// </summary>
        public static SafeMode W3
        {
            get { return w3; }
        }

        /// <summary>
        /// Gets an instance of SafeMode with safe mode on and w=4.
        /// </summary>
        public static SafeMode W4
        {
            get { return w4; }
        }

        // public properties
        /// <summary>
        /// Gets whether safe mode is enabled.
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (isFrozen) { ThrowFrozenException(); }
                enabled = value;
            }
        }

        /// <summary>
        /// Gets whether fsync is true.
        /// </summary>
        public bool FSync
        {
            get { return fsync; }
            set
            {
                if (isFrozen) { ThrowFrozenException(); }
                fsync = value;
                enabled |= value;
            }
        }

        /// <summary>
        /// Gets whether wait for journal commit is true.
        /// </summary>
        public bool J
        {
            get { return j; }
            set
            {
                if (isFrozen) { ThrowFrozenException(); }
                j = value;
                enabled |= value;
            }
        }

        /// <summary>
        /// Gets the w value (the number of write replications that must complete before the server returns).
        /// </summary>
        public int W
        {
            get { return w; }
            set
            {
                if (isFrozen) { ThrowFrozenException(); }
                w = value;
                wmode = null;
                enabled |= (value != 0);
            }
        }

        /// <summary>
        /// Gets the w mode (the w mode determines which write replications must complete before the server returns).
        /// </summary>
        public string WMode
        {
            get { return wmode; }
            set
            {
                if (isFrozen) { ThrowFrozenException(); }
                w = 0;
                wmode = value;
                enabled |= (value != null);
            }
        }

        /// <summary>
        /// Gets the wtimeout value (the timeout before which the server must return).
        /// </summary>
        public TimeSpan WTimeout
        {
            get { return wtimeout; }
            set
            {
                if (isFrozen) { ThrowFrozenException(); }
                wtimeout = value;
            }
        }

        // public operators
        /// <summary>
        /// Compares two SafeMode values.
        /// </summary>
        /// <param name="lhs">The first SafeMode value.</param>
        /// <param name="rhs">The other SafeMode value.</param>
        /// <returns>True if the values are equal (or both null).</returns>
        public static bool operator ==(SafeMode lhs, SafeMode rhs)
        {
            return object.Equals(lhs, rhs);
        }

        /// <summary>
        /// Compares two SafeMode values.
        /// </summary>
        /// <param name="lhs">The first SafeMode value.</param>
        /// <param name="rhs">The other SafeMode value.</param>
        /// <returns>True if the values are not equal (or one is null and the other is not).</returns>
        public static bool operator !=(SafeMode lhs, SafeMode rhs)
        {
            return !(lhs == rhs);
        }

        // public static methods
        /// <summary>
        /// Creates a SafeMode instance (or returns an existing instance).
        /// </summary>
        /// <param name="enabled">Whether safe mode is enabled.</param>
        /// <returns>A SafeMode instance.</returns>
        public static SafeMode Create(bool enabled)
        {
            return Create(enabled, false);
        }

        /// <summary>
        /// Creates a SafeMode instance (or returns an existing instance).
        /// </summary>
        /// <param name="enabled">Whether safe mode is enabled.</param>
        /// <param name="fsync">Whether fysnc is true.</param>
        /// <returns>A SafeMode instance.</returns>
        public static SafeMode Create(bool enabled, bool fsync)
        {
            return Create(enabled, fsync, 0);
        }

        /// <summary>
        /// Creates a SafeMode instance (or returns an existing instance).
        /// </summary>
        /// <param name="enabled">Whether safe mode is enabled.</param>
        /// <param name="fsync">Whether fysnc is true.</param>
        /// <param name="w">The number of write replications that should be completed before server returns.</param>
        /// <returns>A SafeMode instance.</returns>
        public static SafeMode Create(bool enabled, bool fsync, int w)
        {
            return Create(enabled, fsync, w, TimeSpan.Zero);
        }

        /// <summary>
        /// Creates a SafeMode instance (or returns an existing instance).
        /// </summary>
        /// <param name="enabled">Whether safe mode is enabled.</param>
        /// <param name="fsync">Whether fysnc is true.</param>
        /// <param name="w">The number of write replications that should be completed before server returns.</param>
        /// <param name="wtimeout">The timeout for each operation.</param>
        /// <returns>A SafeMode instance.</returns>
        public static SafeMode Create(bool enabled, bool fsync, int w, TimeSpan wtimeout)
        {
            if (!fsync && wtimeout == TimeSpan.Zero)
            {
                if (enabled)
                {
                    switch (w)
                    {
                        case 2: return w2;
                        case 3: return w3;
                        case 4: return w4;
                        default: return new SafeMode(true, false, w);
                    }
                }
                else if (w == 0)
                {
                    return @false;
                }
            }
            return new SafeMode(enabled, fsync, w, wtimeout);
        }

        /// <summary>
        /// Creates a SafeMode instance (or returns an existing instance).
        /// </summary>
        /// <param name="w">The number of write replications that should be completed before server returns.</param>
        /// <returns>A SafeMode instance.</returns>
        public static SafeMode Create(int w)
        {
            return Create(w, TimeSpan.Zero);
        }

        /// <summary>
        /// Creates a SafeMode instance (or returns an existing instance).
        /// </summary>
        /// <param name="w">The number of write replications that should be completed before server returns.</param>
        /// <param name="wtimeout">The timeout for each operation.</param>
        /// <returns>A SafeMode instance.</returns>
        public static SafeMode Create(int w, TimeSpan wtimeout)
        {
            return Create(true, false, w, wtimeout);
        }

        // public methods
        /// <summary>
        /// Creates a clone of the SafeMode.
        /// </summary>
        /// <returns>A clone of the SafeMode.</returns>
        public SafeMode Clone()
        {
            return new SafeMode(this);
        }

        /// <summary>
        /// Compares two SafeMode values.
        /// </summary>
        /// <param name="obj">The other SafeMode value.</param>
        /// <returns>True if the values are equal.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as SafeMode); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Compares two SafeMode values.
        /// </summary>
        /// <param name="rhs">The other SafeMode value.</param>
        /// <returns>True if the values are equal.</returns>
        public bool Equals(SafeMode rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }
            return
                this.enabled == rhs.enabled &&
                this.fsync == rhs.fsync &&
                this.j == rhs.j &&
                this.w == rhs.w &&
                this.wmode == rhs.wmode &&
                this.wtimeout == rhs.wtimeout;
        }

        /// <summary>
        /// Freezes the SafeMode.
        /// </summary>
        /// <returns>The frozen SafeMode.</returns>
        public SafeMode Freeze()
        {
            if (!isFrozen)
            {
                frozenHashCode = GetHashCodeHelper();
                isFrozen = true;
            }
            return this;
        }

        /// <summary>
        /// Returns a frozen copy of the SafeMode.
        /// </summary>
        /// <returns>A frozen copy of the SafeMode.</returns>
        public SafeMode FrozenCopy()
        {
            if (isFrozen)
            {
                return this;
            }
            else
            {
                return Clone().Freeze();
            }
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            if (isFrozen)
            {
                return frozenHashCode;
            }
            else
            {
                return GetHashCodeHelper();
            }
        }

        /// <summary>
        /// Returns a string representation of the SafeMode.
        /// </summary>
        /// <returns>A string representation of the SafeMode.</returns>
        public override string ToString()
        {
            if (enabled)
            {
                var sb = new StringBuilder("safe=true");
                if (fsync)
                {
                    sb.Append(",fsync=true");
                }
                if (j)
                {
                    sb.Append(",j=true");
                }
                if (w != 0 || wmode != null)
                {
                    if (w != 0)
                    {
                        sb.AppendFormat(",w={0}", w);
                    }
                    if (wmode != null)
                    {
                        sb.AppendFormat(",wmode=\"{0}\"", wmode);
                    }
                    if (wtimeout != TimeSpan.Zero)
                    {
                        sb.AppendFormat(",wtimeout={0}", wtimeout);
                    }
                }
                return sb.ToString();
            }
            else
            {
                return "safe=false";
            }
        }

        // private methods
        private int GetHashCodeHelper()
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + enabled.GetHashCode();
            hash = 37 * hash + fsync.GetHashCode();
            hash = 37 * hash + j.GetHashCode();
            hash = 37 * hash + w.GetHashCode();
            hash = 37 * hash + ((wmode == null) ? 0 : wmode.GetHashCode());
            hash = 37 * hash + wtimeout.GetHashCode();
            return hash;
        }

        private void ThrowFrozenException()
        {
            throw new InvalidOperationException("SafeMode has been frozen and no further changes are allowed.");
        }
    }
}
