/* Copyright 2010-2013 10gen Inc.
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
    /// Represents a credentials store that contains credentials for different databases.
    /// </summary>
    public class MongoCredentialsStore : IEnumerable<MongoCredentials>
    {
        // private fields
        private readonly List<MongoCredentials> _credentialsList = new List<MongoCredentials>();
        private readonly HashSet<string> _sources = new HashSet<string>();

        private int _frozenHashCode;
        private string _frozenStringRepresentation;
        private bool _isFrozen;

        // constructors
        /// <summary>
        /// Creates a new instance of the MongoCredentialsStore class.
        /// </summary>
        public MongoCredentialsStore()
        {
        }

        // public properties
        /// <summary>
        /// Gets the number of credentials in the store.
        /// </summary>
        public int Count
        {
            get { return _credentialsList.Count; }
        }

        /// <summary>
        /// Gets whether the credentials store has been frozen to prevent further changes.
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
        }

        // public methods
        /// <summary>
        /// Adds the specified credentials.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        public void Add(MongoCredentials credentials)
        {
            if (credentials == null)
            {
                throw new ArgumentNullException("credentials");
            }
            if (_sources.Contains(credentials.Source))
            {
                throw new ArgumentException("The server currently requires that each credentials provided be from a separate source. ");
            }
            if (_isFrozen) { throw new InvalidOperationException("MongoCredentialsStore is frozen."); }

            _credentialsList.Add(credentials);
            _sources.Add(credentials.Source);
        }

        /// <summary>
        /// Creates a clone of the credentials store.
        /// </summary>
        /// <returns>A clone of the credentials store.</returns>
        public MongoCredentialsStore Clone()
        {
            var clone = new MongoCredentialsStore();
            foreach (var credentials in _credentialsList)
            {
                clone.Add(credentials);
            }
            return clone;
        }

        /// <summary>
        /// Compares this credentials store to another credentials store.
        /// </summary>
        /// <param name="rhs">The other credentials store.</param>
        /// <returns>True if the two credentials stores are equal.</returns>
        public bool Equals(MongoCredentialsStore rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) 
            {
                return false;
            }

            return _credentialsList.SequenceEqual(rhs._credentialsList);
        }

        /// <summary>
        /// Compares this credentials store to another credentials store.
        /// </summary>
        /// <param name="obj">The other credentials store.</param>
        /// <returns>True if the two credentials stores are equal.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as MongoCredentialsStore); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Freezes the credentials store.
        /// </summary>
        /// <returns>The frozen credentials store.</returns>
        public MongoCredentialsStore Freeze()
        {
            if (!_isFrozen)
            {
                _frozenHashCode = GetHashCode();
                _frozenStringRepresentation = ToString();
                _isFrozen = true;
            }
            return this;
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<MongoCredentials> GetEnumerator()
        {
            return _credentialsList.GetEnumerator();
        }

        /// <summary>
        /// Gets the hashcode for the credentials store.
        /// </summary>
        /// <returns>The hashcode.</returns>
        public override int GetHashCode()
        {
            if (_isFrozen)
            {
                return _frozenHashCode;
            }

            // see Effective Java by Joshua Bloch
            int hash = 17;
            foreach (var credentials in _credentialsList)
            {
                hash = 37 * hash + credentials.GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the credentials store.
        /// </summary>
        /// <returns>A string representation of the credentials store.</returns>
        public override string ToString()
        {
            if (_isFrozen)
            {
                return _frozenStringRepresentation;
            }

            return string.Format("{{{0}}}", string.Join(",", _credentialsList.Select(c => c.ToString()).ToArray()));
        }

        // explicit methods
        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _credentialsList.GetEnumerator();
        }
    }
}
