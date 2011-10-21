﻿/* Copyright 2010-2011 10gen Inc.
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

namespace MongoDB.Bson.IO {
    /// <summary>
    /// Represents settings for a BsonWriter.
    /// </summary>
    [Serializable]
    public abstract class BsonWriterSettings {
        #region protected fields
        /// <summary>
        /// The representation for Guids.
        /// </summary>
        protected GuidRepresentation guidRepresentation = BsonDefaults.GuidRepresentation;
        /// <summary>
        /// Whether the settings are frozen.
        /// </summary>
        protected bool isFrozen;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonWriterSettings class.
        /// </summary>
        protected BsonWriterSettings() {
        }

        /// <summary>
        /// Initializes a new instance of the BsonWriterSettings class.
        /// </summary>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        protected BsonWriterSettings(
            GuidRepresentation guidRepresentation
        ) {
            this.guidRepresentation = guidRepresentation;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets or sets the representation for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation {
            get { return guidRepresentation; }
            set {
                if (isFrozen) { ThrowFrozenException(); }
                guidRepresentation = value;
            }
        }

        /// <summary>
        /// Gets whether the settings are frozen.
        /// </summary>
        public bool IsFrozen {
            get { return isFrozen; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public BsonWriterSettings Clone() {
            return CloneImplementation();
        }

        /// <summary>
        /// Freezes the settings.
        /// </summary>
        /// <returns>The frozen settings.</returns>
        public BsonWriterSettings Freeze() {
            isFrozen = true;
            return this;
        }

        /// <summary>
        /// Returns a frozen copy of the settings.
        /// </summary>
        /// <returns>A frozen copy of the settings.</returns>
        public BsonWriterSettings FrozenCopy() {
            if (isFrozen) {
                return this;
            } else {
                return Clone().Freeze();
            }
        }
        #endregion

        #region protected methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        protected abstract BsonWriterSettings CloneImplementation();

        /// <summary>
        /// Throws an InvalidOperationException when an attempt is made to change a setting after the settings are frozen.
        /// </summary>
        protected void ThrowFrozenException() {
            var message = string.Format("{0} is frozen.", this.GetType().Name);
            throw new InvalidOperationException(message);
        }
        #endregion
    }
}
