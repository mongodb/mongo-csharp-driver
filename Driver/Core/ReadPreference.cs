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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents read preference modes.
    /// </summary>
    public enum ReadPreferenceMode
    {
        /// <summary>
        /// Use primary only.
        /// </summary>
        Primary,
        /// <summary>
        /// Use primary if possible, otherwise a secondary.
        /// </summary>
        PrimaryPreferred,
        /// <summary>
        /// Use secondary only.
        /// </summary>
        Secondary,
        /// <summary>
        /// Use a secondary if possible, otherwise primary.
        /// </summary>
        SecondaryPreferred,
        /// <summary>
        /// Use any near by server, primary or secondary.
        /// </summary>
        Nearest
    }

    /// <summary>
    /// Represents read preferences.
    /// </summary>
    public class ReadPreference : IEquatable<ReadPreference>
    {
        // private static fields
        private static readonly ReadPreference __nearest = new ReadPreference(ReadPreferenceMode.Nearest).Freeze();
        private static readonly ReadPreference __primary = new ReadPreference(ReadPreferenceMode.Primary).Freeze();
        private static readonly ReadPreference __primaryPreferred = new ReadPreference(ReadPreferenceMode.PrimaryPreferred).Freeze();
        private static readonly ReadPreference __secondary = new ReadPreference(ReadPreferenceMode.Secondary).Freeze();
        private static readonly ReadPreference __secondaryPreferred = new ReadPreference(ReadPreferenceMode.SecondaryPreferred).Freeze();

        // private fields
        private readonly Random _random = new Random();
        private readonly object _randomLock = new object();
        private ReadPreferenceMode _readPreferenceMode;
        private List<ReplicaSetTagSet> _tagSets;
        private ReadOnlyCollection<ReplicaSetTagSet> _tagSetsReadOnly;
        private bool _isFrozen;
        private int _frozenHashCode;

        // constructors
        /// <summary>
        /// Initializes a new instance of the ReadPreference class.
        /// </summary>
        public ReadPreference()
        {
            _readPreferenceMode = ReadPreferenceMode.Primary;
        }

        /// <summary>
        /// Initializes a new instance of the ReadPreference class.
        /// </summary>
        /// <param name="readPreference">A read preference</param>
        public ReadPreference(ReadPreference readPreference)
        {
            _readPreferenceMode = readPreference._readPreferenceMode;
            _tagSets = new List<ReplicaSetTagSet>(readPreference._tagSets);
            _tagSetsReadOnly = _tagSets.AsReadOnly();
        }

        /// <summary>
        /// Initializes a new instance of the ReadPreference class.
        /// </summary>
        /// <param name="readPreferenceMode">The read preference mode.</param>
        public ReadPreference(ReadPreferenceMode readPreferenceMode)
        {
            _readPreferenceMode = readPreferenceMode;
        }

        /// <summary>
        /// Initializes a new instance of the ReadPreference class.
        /// </summary>
        /// <param name="readPreferenceMode">The read preference mode.</param>
        /// <param name="tagSets">The tag sets.</param>
        public ReadPreference(ReadPreferenceMode readPreferenceMode, IEnumerable<ReplicaSetTagSet> tagSets)
        {
            _readPreferenceMode = readPreferenceMode;
            _tagSets = new List<ReplicaSetTagSet>(tagSets);
            _tagSetsReadOnly = _tagSets.AsReadOnly();
        }

        // static properties
        /// <summary>
        /// Gets a read preference value for nearest.
        /// </summary>
        public static ReadPreference Nearest
        {
            get { return __nearest; }
        }

        /// <summary>
        /// Gets a read preference value for primary preferred.
        /// </summary>
        public static ReadPreference PrimaryPreferred
        {
            get { return __primaryPreferred; }
        }

        /// <summary>
        /// Gets a read preference value for primary.
        /// </summary>
        public static ReadPreference Primary
        {
            get { return __primary; }
        }

        /// <summary>
        /// Gets a read preference value for secondary.
        /// </summary>
        public static ReadPreference Secondary
        {
            get { return __secondary; }
        }

        /// <summary>
        /// Gets a read preference value for secondary preferred.
        /// </summary>
        public static ReadPreference SecondaryPreferred
        {
            get { return __secondaryPreferred; }
        }

        // public properties
        /// <summary>
        /// Gets whether the read preference has been frozen to prevent further changes.
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
        }

        /// <summary>
        /// Gets the read preference mode.
        /// </summary>
        public ReadPreferenceMode ReadPreferenceMode
        {
            get { return _readPreferenceMode; }
            set
            {
                if (_isFrozen) { ThrowFrozenException(); }
                _readPreferenceMode = value;
            }
        }

        /// <summary>
        /// Gets the tag sets.
        /// </summary>
        public IEnumerable<ReplicaSetTagSet> TagSets
        {
            get { return _tagSetsReadOnly; }
            set
            {
                if (_isFrozen) { ThrowFrozenException(); }
                _tagSets = new List<ReplicaSetTagSet>(value);
                _tagSetsReadOnly = _tagSets.AsReadOnly();
            }
        }

        // public operators
        /// <summary>
        /// Determines whether two specified ReadPreference objects have different values.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is different from the value of rhs; otherwise, false.</returns>
        public static bool operator !=(ReadPreference lhs, ReadPreference rhs)
        {
            return !ReadPreference.Equals(lhs, rhs);
        }

        /// <summary>
        /// Determines whether two specified ReadPreference objects have the same value.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is the same as the value of rhs; otherwise, false.</returns>
        public static bool operator ==(ReadPreference lhs, ReadPreference rhs)
        {
            return ReadPreference.Equals(lhs, rhs);
        }

        // public static methods
        /// <summary>
        /// Determines whether two specified ReadPreference objects have the same value.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is the same as the value of rhs; otherwise, false.</returns>
        public static bool Equals(ReadPreference lhs, ReadPreference rhs)
        {
            if ((object)lhs == null) { return (object)rhs == null; }
            return lhs.Equals(rhs);
        }


        // internal static methods
        internal static ReadPreference FromSlaveOk(bool slaveOk)
        {
            return slaveOk ? ReadPreference.SecondaryPreferred : ReadPreference.Primary;
        }

        // public methods
        /// <summary>
        /// Add a new tag set to the existing tag sets.
        /// </summary>
        /// <param name="tagSet">The new tag set.</param>
        /// <returns>The ReadPreference so calls to AddTagSet can be chained.</returns>
        public ReadPreference AddTagSet(ReplicaSetTagSet tagSet)
        {
            if (_isFrozen) { ThrowFrozenException(); }
            if (_tagSets == null)
            {
                _tagSets = new List<ReplicaSetTagSet>();
                _tagSetsReadOnly = _tagSets.AsReadOnly();
            }
            _tagSets.Add(tagSet);
            return this;
        }

        /// <summary>
        /// Creates a clone of the ReadPreference.
        /// </summary>
        /// <returns>A clone of the ReadPreference.</returns>
        public ReadPreference Clone()
        {
            return new ReadPreference(this);
        }

        /// <summary>
        /// Determines whether this instance and another specified ReadPreference object have the same value.
        /// </summary>
        /// <param name="rhs">The ReadPreference object to compare to this instance.</param>
        /// <returns>True if the value of the rhs parameter is the same as this instance; otherwise, false.</returns>
        public bool Equals(ReadPreference rhs)
        {
            if ((object)rhs == null || GetType() != rhs.GetType()) { return false; }
            if ((object)this == (object)rhs) { return true; }
            return
                _readPreferenceMode == rhs._readPreferenceMode &&
                ((object)_tagSets == (object)rhs._tagSets || _tagSets != null && rhs._tagSets != null && _tagSets.SequenceEqual(rhs._tagSets));
        }

        /// <summary>
        /// Determines whether this instance and a specified object, which must also be a ReadPreference object, have the same value.
        /// </summary>
        /// <param name="obj">The ReadPreference object to compare to this instance.</param>
        /// <returns>True if obj is a ReadPreference object and its value is the same as this instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ReadPreference); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Freezes the ReadPreference.
        /// </summary>
        /// <returns>The frozen ReadPreference.</returns>
        public ReadPreference Freeze()
        {
            if (!_isFrozen)
            {
                if (_tagSets != null) { _tagSets.ForEach(s => s.Freeze()); }
                _frozenHashCode = GetHashCode();
                _isFrozen = true;
            }
            return this;
        }

        /// <summary>
        /// Returns a frozen copy of the ReadPreference.
        /// </summary>
        /// <returns>A frozen copy of the ReadPreference.</returns>
        public ReadPreference FrozenCopy()
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
        /// Returns the hash code for this Class1 object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            if (_isFrozen)
            {
                return _frozenHashCode;
            }

            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + _readPreferenceMode.GetHashCode();
            hash = 37 * hash + ((_tagSets == null) ? 0 : _tagSets.GetHashCode());
            return hash;
        }

        /// <summary>
        /// Tests whether the server instance matches the read preference.
        /// </summary>
        /// <param name="instance">The server instance.</param>
        /// <returns>True if the server instance matches the read preferences.</returns>
        public bool MatchesInstance(MongoServerInstance instance)
        {
            switch (_readPreferenceMode)
            {
                case ReadPreferenceMode.Primary:
                    if (!instance.IsPrimary) { return false; }
                    break;
                case ReadPreferenceMode.Secondary:
                    if (!instance.IsSecondary) { return false; }
                    break;
                case ReadPreferenceMode.PrimaryPreferred:
                case ReadPreferenceMode.SecondaryPreferred:
                case ReadPreferenceMode.Nearest:
                    if (!instance.IsPrimary && !instance.IsSecondary) { return false; }
                    break;
                default:
                    throw new MongoInternalException("Invalid ReadPreferenceMode");
            }

            if (_tagSets != null && instance.Type == MongoServerInstanceType.ReplicaSetMember)
            {
                var someSetMatches = false;
                foreach (var tagSet in _tagSets)
                {
                    if (tagSet.MatchesInstance(instance))
                    {
                        someSetMatches = true;
                        break;
                    }
                }

                if (!someSetMatches)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a string representation of the ReadPreference.
        /// </summary>
        /// <returns>A string representation of the user.</returns>
        public override string ToString()
        {
            if (_tagSets == null || _tagSets.Count == 0)
            {
                return _readPreferenceMode.ToString();
            }
            else
            {
                return string.Format("{0}(tags: {1})}", _readPreferenceMode, _tagSets);
            }
        }

        // internal methods
        internal MongoServerInstance ChooseServerInstance(IEnumerable<MongoServerInstance> connectedInstancesByPingTime)
        {
            // tags are not evaluated for a primary
            if (_readPreferenceMode == ReadPreferenceMode.Primary || _readPreferenceMode == ReadPreferenceMode.PrimaryPreferred)
            {
                foreach (var instance in connectedInstancesByPingTime)
                {
                    if (instance.IsPrimary)
                    {
                        return instance;
                    }
                }
                if (_readPreferenceMode == ReadPreferenceMode.Primary)
                {
                    return null;
                }
            }

            List<MongoServerInstance> matchingInstances = new List<MongoServerInstance>();
            TimeSpan maxPingTime = TimeSpan.MaxValue;
            foreach (var instance in connectedInstancesByPingTime)
            {
                if (instance.PingTime > maxPingTime)
                {
                    break; // any subsequent instances will also exceed maxPingTime
                }
                if (MatchesInstance(instance))
                {
                    if (maxPingTime == TimeSpan.MaxValue)
                    {
                        var secondaryAcceptableLatency = TimeSpan.FromMilliseconds(15);
                        maxPingTime = instance.PingTime + secondaryAcceptableLatency;
                    }
                    matchingInstances.Add(instance);
                }
            }

            if (matchingInstances.Count == 0)
            {
                return null;
            }

            if (_readPreferenceMode == ReadPreferenceMode.SecondaryPreferred)
            {
                MongoServerInstance primary = null;
                foreach (var instance in matchingInstances)
                {
                    if (instance.IsPrimary)
                    {
                        primary = instance;
                        break;
                    }
                }

                if (primary != null)
                {
                    if (matchingInstances.Count == 1)
                    {
                        return primary;
                    }
                    else
                    {
                        matchingInstances.Remove(primary);
                    }
                }
            }

            switch (matchingInstances.Count)
            {
                case 0:
                    return null;
                case 1:
                    return matchingInstances[0];
                default:
                    int randomIndex;
                    lock (_randomLock)
                    {
                        randomIndex = _random.Next(matchingInstances.Count);
                    }
                    return matchingInstances[randomIndex]; // random load balancing
            }
        }

        internal bool ToSlaveOk()
        {
            return _readPreferenceMode != ReadPreferenceMode.Primary;
        }

        // private methods
        private void ThrowFrozenException()
        {
            throw new InvalidOperationException("ReadPreference has been frozen and no further changes are allowed.");
        }
    }
}
