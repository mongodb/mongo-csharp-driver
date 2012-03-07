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

using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// The settings used to access a database.
    /// </summary>
    public class MongoDatabaseSettings
    {
        // private fields
        private string _databaseName;
        private MongoCredentials _credentials;
        private GuidRepresentation _guidRepresentation;
        private SafeMode _safeMode;
        private bool _slaveOk;
        // the following fields are set when Freeze is called
        private bool _isFrozen;
        private int _frozenHashCode;
        private string _frozenStringRepresentation;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoDatabaseSettings.
        /// </summary>
        /// <param name="server">The server to inherit settings from.</param>
        /// <param name="databaseName">The name of the database.</param>
        public MongoDatabaseSettings(MongoServer server, string databaseName)
        {
            var serverSettings = server.Settings;
            _databaseName = databaseName;
            _credentials = serverSettings.GetCredentials(databaseName);
            _guidRepresentation = serverSettings.GuidRepresentation;
            _safeMode = serverSettings.SafeMode;
            _slaveOk = serverSettings.SlaveOk;
        }

        /// <summary>
        /// Creates a new instance of MongoDatabaseSettings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="credentials">The credentials to access the database.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        /// <param name="safeMode">The safe mode to use.</param>
        /// <param name="slaveOk">Whether queries should be sent to secondary servers.</param>
        public MongoDatabaseSettings(
            string databaseName,
            MongoCredentials credentials,
            GuidRepresentation guidRepresentation,
            SafeMode safeMode,
            bool slaveOk)
        {
            if (databaseName == "admin" && credentials != null && !credentials.Admin)
            {
                throw new ArgumentOutOfRangeException("Credentials for the admin database must have the admin flag set to true.");
            }
            _databaseName = databaseName;
            _credentials = credentials;
            _guidRepresentation = guidRepresentation;
            _safeMode = safeMode;
            _slaveOk = slaveOk;
        }

        // public properties
        /// <summary>
        /// Gets or sets the credentials to access the database.
        /// </summary>
        public MongoCredentials Credentials
        {
            get { return _credentials; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoDatabaseSettings is frozen."); }
                _credentials = value;
            }
        }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        public string DatabaseName
        {
            get { return _databaseName; }
        }

        /// <summary>
        /// Gets or sets the representation to use for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation
        {
            get { return _guidRepresentation; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoDatabaseSettings is frozen."); }
                _guidRepresentation = value;
            }
        }

        /// <summary>
        /// Gets whether the settings have been frozen to prevent further changes.
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
        }

        /// <summary>
        /// Gets or sets the SafeMode to use.
        /// </summary>
        public SafeMode SafeMode
        {
            get { return _safeMode; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoDatabaseSettings is frozen."); }
                _safeMode = value;
            }
        }

        /// <summary>
        /// Gets or sets whether queries should be sent to secondary servers.
        /// </summary>
        public bool SlaveOk
        {
            get { return _slaveOk; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoDatabaseSettings is frozen."); }
                _slaveOk = value;
            }
        }

        // public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public MongoDatabaseSettings Clone()
        {
            return new MongoDatabaseSettings(_databaseName, _credentials, _guidRepresentation, _safeMode, _slaveOk);
        }

        /// <summary>
        /// Compares two MongoDatabaseSettings instances.
        /// </summary>
        /// <param name="obj">The other instance.</param>
        /// <returns>True if the two instances are equal.</returns>
        public override bool Equals(object obj)
        {
            var rhs = obj as MongoDatabaseSettings;
            if (rhs == null)
            {
                return false;
            }
            else
            {
                if (_isFrozen && rhs._isFrozen)
                {
                    return _frozenStringRepresentation == rhs._frozenStringRepresentation;
                }
                else
                {
                    return
                        _databaseName == rhs._databaseName &&
                        _credentials == rhs._credentials &&
                        _guidRepresentation == rhs._guidRepresentation &&
                        _safeMode == rhs._safeMode &&
                        _slaveOk == rhs._slaveOk;
                }
            }
        }

        /// <summary>
        /// Freezes the settings.
        /// </summary>
        /// <returns>The frozen settings.</returns>
        public MongoDatabaseSettings Freeze()
        {
            if (!_isFrozen)
            {
                _safeMode = _safeMode.FrozenCopy();
                _frozenHashCode = GetHashCode();
                _frozenStringRepresentation = ToString();
                _isFrozen = true;
            }
            return this;
        }

        /// <summary>
        /// Returns a frozen copy of the settings.
        /// </summary>
        /// <returns>A frozen copy of the settings.</returns>
        public MongoDatabaseSettings FrozenCopy()
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
            hash = 37 * hash + ((_databaseName == null) ? 0 : _databaseName.GetHashCode());
            hash = 37 * hash + ((_credentials == null) ? 0 : _credentials.GetHashCode());
            hash = 37 * hash + _guidRepresentation.GetHashCode();
            hash = 37 * hash + ((_safeMode == null) ? 0 : _safeMode.GetHashCode());
            hash = 37 * hash + _slaveOk.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the settings.
        /// </summary>
        /// <returns>A string representation of the settings.</returns>
        public override string ToString()
        {
            if (_isFrozen)
            {
                return _frozenStringRepresentation;
            }

            return string.Format(
                "DatabaseName={0};Credentials={1};GuidRepresentation={2};SafeMode={3};SlaveOk={4}",
                _databaseName, _credentials, _guidRepresentation, _safeMode, _slaveOk);
        }
    }
}
