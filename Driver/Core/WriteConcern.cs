/* Copyright 2010-2012 10gen Inc.
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
using System.Xml;

using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the different WriteConcerns that can be used.
    /// </summary>
    [Serializable]
    public class WriteConcern : IEquatable<WriteConcern>
    {
        // private static fields
        private readonly static WriteConcern __errors = new WriteConcern { FireAndForget = false }.Freeze();
        private readonly static WriteConcern __none = new WriteConcern { FireAndForget = true }.Freeze();
        private readonly static WriteConcern __w2 = new WriteConcern { W = 2 }.Freeze();
        private readonly static WriteConcern __w3 = new WriteConcern { W = 3 }.Freeze();
        private readonly static WriteConcern __w4 = new WriteConcern { W = 4 }.Freeze();
        private readonly static WriteConcern __wmajority = new WriteConcern { W = "majority" }.Freeze();

        // private fields
        private bool _fireAndForget;
        private bool? _fsync;
        private bool? _journal;
        private WValue _w;
        private TimeSpan? _wTimeout;

        private bool _isFrozen;
        private int _frozenHashCode;

        // public static properties
        /// <summary>
        /// Gets an instance of WriteConcern that checks for errors.
        /// </summary>
        public static WriteConcern Errors
        {
            get { return __errors; }
        }

        /// <summary>
        /// Gets an instance of WriteConcern that doesn't check for errors.
        /// </summary>
        public static WriteConcern None
        {
            get { return __none; }
        }

        /// <summary>
        /// Gets an instance of WriteConcern where w=2.
        /// </summary>
        public static WriteConcern W2
        {
            get { return __w2; }
        }

        /// <summary>
        /// Gets an instance of WriteConcern where w=3.
        /// </summary>
        public static WriteConcern W3
        {
            get { return __w3; }
        }

        /// <summary>
        /// Gets an instance of WriteConcern where w=4.
        /// </summary>
        public static WriteConcern W4
        {
            get { return __w4; }
        }

        /// <summary>
        /// Gets an instance of WriteConcern where w="majority".
        /// </summary>
        public static WriteConcern WMajority
        {
            get { return __wmajority; }
        }

        // public properties
        /// <summary>
        /// Gets or sets whether WriteConcern is fire and forget.
        /// </summary>
        public bool FireAndForget
        {
            get { return _fireAndForget; }
            set
            {
                if (_isFrozen) { ThrowFrozenException(); }
                if (value && AnyWriteConcernSettingsAreSet())
                {
                    throw new InvalidOperationException("FireAndForget cannot be set to true if any other WriteConcern values have been set.");
                }
                _fireAndForget = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to wait for an fsync to complete.
        /// </summary>
        public bool? FSync
        {
            get { return _fsync; }
            set
            {
                if (_isFrozen) { ThrowFrozenException(); }
                if (value != null) { EnsureFireAndForgetIsNotTrue("FSync"); }
                _fsync = value;
            }
        }

        /// <summary>
        /// Gets whether this instance is frozen.
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
        }

        /// <summary>
        /// Gets or sets whether to wait for journal commit.
        /// </summary>
        public bool? Journal
        {
            get { return _journal; }
            set
            {
                if (_isFrozen) { ThrowFrozenException(); }
                if (value != null) { EnsureFireAndForgetIsNotTrue("Journal"); }
                _journal = value;
            }
        }

        /// <summary>
        /// Gets or sets the w value.
        /// </summary>
        public WValue W
        {
            get { return _w; }
            set
            {
                if (_isFrozen) { ThrowFrozenException(); }
                if (value != null) { EnsureFireAndForgetIsNotTrue("W"); }
                _w = value;
            }
        }

        /// <summary>
        /// Gets or sets the wtimeout value (the timeout before which the server must return).
        /// </summary>
        public TimeSpan? WTimeout
        {
            get { return _wTimeout; }
            set
            {
                if (_isFrozen) { ThrowFrozenException(); }
                if (value != null) { EnsureFireAndForgetIsNotTrue("WTimeout"); }
                _wTimeout = value;
            }
        }

        // public operators
        /// <summary>
        /// Determines whether two specified WriteConcern objects have different values.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is different from the value of rhs; otherwise, false.</returns>
        public static bool operator !=(WriteConcern lhs, WriteConcern rhs)
        {
            return !WriteConcern.Equals(lhs, rhs);
        }

        /// <summary>
        /// Determines whether two specified WriteConcern objects have the same value.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is the same as the value of rhs; otherwise, false.</returns>
        public static bool operator ==(WriteConcern lhs, WriteConcern rhs)
        {
            return WriteConcern.Equals(lhs, rhs);
        }

        // public static methods
        /// <summary>
        /// Determines whether two specified WriteConcern objects have the same value.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is the same as the value of rhs; otherwise, false.</returns>
        public static bool Equals(WriteConcern lhs, WriteConcern rhs)
        {
            if ((object)lhs == null) { return (object)rhs == null; }
            return lhs.Equals(rhs);
        }

        // public methods
        /// <summary>
        /// Creates a clone of the WriteConcern.
        /// </summary>
        /// <returns>A clone of the WriteConcern.</returns>
        public WriteConcern Clone()
        {
            var clone = new WriteConcern();
            clone._fireAndForget = _fireAndForget;
            clone._fsync = _fsync;
            clone._journal = _journal;
            clone._w = _w;
            clone._wTimeout = _wTimeout;
            return clone;
        }

        /// <summary>
        /// Determines whether this instance and a specified object, which must also be a WriteConcern object, have the same value.
        /// </summary>
        /// <param name="obj">The WriteConcern object to compare to this instance.</param>
        /// <returns>True if obj is a WriteConcern object and its value is the same as this instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as WriteConcern); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Determines whether this instance and another specified WriteConcern object have the same value.
        /// </summary>
        /// <param name="rhs">The WriteConcern object to compare to this instance.</param>
        /// <returns>True if the value of the rhs parameter is the same as this instance; otherwise, false.</returns>
        public bool Equals(WriteConcern rhs)
        {
            if ((object)rhs == null || GetType() != rhs.GetType()) { return false; }
            if ((object)this == (object)rhs) { return true; }
            return
                _fireAndForget == rhs._fireAndForget &&
                _fsync == rhs._fsync &&
                _journal == rhs._journal &&
                _w == rhs._w &&
                _wTimeout == rhs._wTimeout;
        }

        /// <summary>
        /// Freezes the WriteConcern.
        /// </summary>
        /// <returns>The frozen WriteConcern.</returns>
        public WriteConcern Freeze()
        {
            if (!_isFrozen)
            {
                _frozenHashCode = GetHashCode();
                _isFrozen = true;
            }
            return this;
        }

        /// <summary>
        /// Returns a frozen copy of the WriteConcern.
        /// </summary>
        /// <returns>A frozen copy of the WriteConcern.</returns>
        public WriteConcern FrozenCopy()
        {
            if (_isFrozen)
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
            if (_isFrozen)
            {
                return _frozenHashCode;
            }

            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + _fireAndForget.GetHashCode();
            hash = 37 * hash + _fsync.GetHashCode();
            hash = 37 * hash + _journal.GetHashCode();
            hash = 37 * hash + ((_w == null) ? 0 : _w.GetHashCode());
            hash = 37 * hash + _wTimeout.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the WriteConcern.
        /// </summary>
        /// <returns>A string representation of the WriteConcern.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("fireAndForget={0}", XmlConvert.ToString(_fireAndForget));
            if (!_fireAndForget)
            {
                if (_fsync != null)
                {
                    sb.AppendFormat(",fsync={0}", _fsync.Value);
                }
                if (_journal != null)
                {
                    sb.AppendFormat(",journal={0}", _journal.Value);
                }
                if (_w != null)
                {
                    sb.AppendFormat(",w={0}", _w);
                }
                if (_wTimeout != null)
                {
                    sb.AppendFormat(",wtimeout={0}", MongoUrlBuilder.FormatTimeSpan(_wTimeout.Value));
                }
            }
            return sb.ToString();
        }

        // private methods
        private bool AnyWriteConcernSettingsAreSet()
        {
            return _fsync != null || _journal != null || _w != null || _wTimeout != null;
        }

        private void EnsureFireAndForgetIsNotTrue(string propertyName)
        {
            if (_fireAndForget)
            {
                var message = string.Format("{0} cannot be set when FireAndForget is true.", propertyName);
                throw new InvalidOperationException(message);
            }
        }

        private void ThrowFrozenException()
        {
            throw new InvalidOperationException("WriteConcern has been frozen and no further changes are allowed.");
        }

        // nested types
        /// <summary>
        /// Represents a "w" value in a WriteConcern.
        /// </summary>
        public abstract class WValue
        {
            // implicit conversions
            /// <summary>
            /// Converts an int value to a WValue of type WCount.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>A WValue of type WCount.</returns>
            public static implicit operator WValue(int value)
            {
                return new WCount(value);
            }

            /// <summary>
            /// Convert a string value to a WValue of type WMode.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>A WValue of type WMode.</returns>
            public static implicit operator WValue(string value)
            {
                return (value == null) ? null : new WMode(value);
            }

            // public operators
            /// <summary>
            /// Determines whether two specified WValue objects have the same value.
            /// </summary>
            /// <param name="lhs">A WValue, or null.</param>
            /// <param name="rhs">A WValue, or null.</param>
            /// <returns>True if the value of lhs is the same as the value of rhs; otherwise, false.</returns>
            public static bool operator ==(WValue lhs, WValue rhs)
            {
                return object.Equals(lhs, rhs);
            }

            /// <summary>
            /// Determines whether two specified WValue objects have different values.
            /// </summary>
            /// <param name="lhs">A WValue, or null.</param>
            /// <param name="rhs">A WValue, or null.</param>
            /// <returns>True if the value of lhs is different from the value of rhs; otherwise, false.</returns>
            public static bool operator !=(WValue lhs, WValue rhs)
            {
                return !object.Equals(lhs, rhs);
            }

            // public static methods
            /// <summary>
            /// Converts the string representation of a WValue to a WValue.
            /// </summary>
            /// <param name="s">A string containing the WValue to convert.</param>
            /// <returns>A WValue equivalent to s.</returns>
            public static WValue Parse(string s)
            {
                int n;
                if (int.TryParse(s, out n))
                {
                    return new WCount(n);
                }
                else
                {
                    return new WMode(s);
                }
            }

            // public methods
            /// <summary>
            /// Determines whether this instance of WValue and a specified object, which must also be a WValue object, have the same value.
            /// </summary>
            /// <param name="obj">An object.</param>
            /// <returns>True if obj is a WValue of the same type as this instance and its value is the same as this instance; otherwise, false.</returns>
            public override bool Equals(object obj)
            {
                throw new NotImplementedException("Must be implemented by subclasses.");
            }

            /// <summary>
            /// Returns the hash code for this WValue.
            /// </summary>
            /// <returns>A 32-bit signed integer hash code.</returns>
            public override int GetHashCode()
            {
                throw new NotImplementedException("Must be implemented by subclasses.");
            }

            // internal methods
            internal abstract BsonValue ToBsonValue();
        }

        /// <summary>
        /// Represents an integer "w" value in a WriteConcern.
        /// </summary>
        public class WCount : WValue
        {
            // private fields
            private readonly int _value;

            // constructors
            /// <summary>
            /// Initializes a new instance of the WCount class.
            /// </summary>
            /// <param name="value">The value.</param>
            public WCount(int value)
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "W value must be greater than zero.");
                }
                _value = value;
            }

            // public properties
            /// <summary>
            /// Gets the value.
            /// </summary>
            public int Value
            {
                get { return _value; }
            }

            // public methods
            /// <summary>
            /// Determines whether this instance of WCount and a specified object, which must also be a WCount object, have the same value.
            /// </summary>
            /// <param name="obj">An object.</param>
            /// <returns>True if obj is a WCount and its value is the same as this instance; otherwise, false.</returns>
            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != typeof(WCount)) { return false; }
                return _value == ((WCount)obj).Value;
            }

            /// <summary>
            /// Returns the hash code for this WCount.
            /// </summary>
            /// <returns>A 32-bit signed integer hash code.</returns>
            public override int GetHashCode()
            {
                return _value.GetHashCode();
            }

            /// <summary>
            /// Converts the numeric value of this instance to its equivalent string representation.
            /// </summary>
            /// <returns>The string representation of the value of this instance.</returns>
            public override string ToString()
            {
                return XmlConvert.ToString(_value);
            }

            // internal methods
            internal override BsonValue ToBsonValue()
            {
                return new BsonInt32(_value);
            }
        }

        /// <summary>
        /// Represents a string "w" value in a WriteConcern (the name of a mode).
        /// </summary>
        public class WMode : WValue
        {
            // private fields
            private readonly string _value;

            // constructors
            /// <summary>
            /// Initializes a new instance of the WMode class.
            /// </summary>
            /// <param name="value">The value.</param>
            public WMode(string value)
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _value = value;
            }

            // public properties
            /// <summary>
            /// Gets the value.
            /// </summary>
            public string Value
            {
                get { return _value; }
            }

            // public methods
            /// <summary>
            /// Determines whether this instance of WMode and a specified object, which must also be a WMode object, have the same value.
            /// </summary>
            /// <param name="obj">An object.</param>
            /// <returns>True if obj is a WMode and its value is the same as this instance; otherwise, false.</returns>
            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != typeof(WMode)) { return false; }
                return _value == ((WMode)obj).Value;
            }

            /// <summary>
            /// Returns the hash code for this WMode.
            /// </summary>
            /// <returns>A 32-bit signed integer hash code.</returns>
            public override int GetHashCode()
            {
                return _value.GetHashCode();
            }

            /// <summary>
            /// Converts the numeric value of this instance to its equivalent string representation.
            /// </summary>
            /// <returns>The string representation of the value of this instance.</returns>
            public override string ToString()
            {
                return _value;
            }

            // internal methods
            internal override BsonValue ToBsonValue()
            {
                return new BsonString(_value);
            }
        }
    }
}
