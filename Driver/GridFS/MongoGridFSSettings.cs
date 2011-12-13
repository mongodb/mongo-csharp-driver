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

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents setting for GridFS.
    /// </summary>
    [Serializable]
    public class MongoGridFSSettings : IEquatable<MongoGridFSSettings>
    {
        // private static fields
        private static MongoGridFSSettings defaults = new MongoGridFSSettings();

        // private fields
        private bool isFrozen;
        private string chunksCollectionName = "fs.chunks";
        private int chunkSize = 256 * 1024; // 256KiB
        private string filesCollectionName = "fs.files";
        private string root = "fs";
        private SafeMode safeMode = SafeMode.False;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoGridFSSettings class.
        /// </summary>
        public MongoGridFSSettings()
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoGridFSSettings class.
        /// </summary>
        /// <param name="database">The database from which to inherit some of the settings.</param>
        public MongoGridFSSettings(MongoDatabase database)
        {
            this.chunkSize = MongoGridFSSettings.Defaults.ChunkSize;
            this.root = MongoGridFSSettings.Defaults.Root;
            this.SafeMode = database.Settings.SafeMode;
        }

        /// <summary>
        /// Initializes a new instance of the MongoGridFSSettings class.
        /// </summary>
        /// <param name="chunkSize">The chunk size.</param>
        /// <param name="root">The root collection name.</param>
        /// <param name="safeMode">The safe mode.</param>
        public MongoGridFSSettings(int chunkSize, string root, SafeMode safeMode)
        {
            this.chunkSize = chunkSize;
            this.Root = root; // use property not field
            this.safeMode = safeMode;
        }

        // public static properties
        /// <summary>
        /// Gets or sets the default GridFS settings.
        /// </summary>
        public static MongoGridFSSettings Defaults
        {
            get { return defaults; }
            set { defaults = value; }
        }

        // public properties
        /// <summary>
        /// Gets the chunks collection name.
        /// </summary>
        public string ChunksCollectionName
        {
            get { return chunksCollectionName; }
        }

        /// <summary>
        /// Gets or sets the chunk size.
        /// </summary>
        public int ChunkSize
        {
            get { return chunkSize; }
            set
            {
                if (isFrozen) { ThrowFrozen(); }
                chunkSize = value;
            }
        }

        /// <summary>
        /// Gets the files collection name.
        /// </summary>
        public string FilesCollectionName
        {
            get { return filesCollectionName; }
        }

        /// <summary>
        /// Gets whether the settings are frozen.
        /// </summary>
        public bool IsFrozen
        {
            get { return isFrozen; }
        }

        /// <summary>
        /// Gets or sets the root collection name (the files and chunks collection names are derived from the root).
        /// </summary>
        public string Root
        {
            get { return root; }
            set
            {
                if (isFrozen) { ThrowFrozen(); }
                root = value;
                filesCollectionName = value + ".files";
                chunksCollectionName = value + ".chunks";
            }
        }

        /// <summary>
        /// Gets or sets the safe mode.
        /// </summary>
        public SafeMode SafeMode
        {
            get { return safeMode; }
            set
            {
                if (isFrozen) { ThrowFrozen(); }
                safeMode = value;
            }
        }

        // public operators
        /// <summary>
        /// Compares two MongoGridFSSettings.
        /// </summary>
        /// <param name="lhs">The first MongoGridFSSettings.</param>
        /// <param name="rhs">The other MongoGridFSSettings.</param>
        /// <returns>True if the two MongoGridFSSettings are not equal (or one is null and the other is not).</returns>
        public static bool operator !=(MongoGridFSSettings lhs, MongoGridFSSettings rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compares two MongoGridFSSettings.
        /// </summary>
        /// <param name="lhs">The first MongoGridFSSettings.</param>
        /// <param name="rhs">The other MongoGridFSSettings.</param>
        /// <returns>True if the two MongoGridFSSettings are equal (or both null).</returns>
        public static bool operator ==(MongoGridFSSettings lhs, MongoGridFSSettings rhs)
        {
            return object.Equals(lhs, rhs);
        }

        // public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public MongoGridFSSettings Clone()
        {
            return new MongoGridFSSettings(chunkSize, root, safeMode);
        }

        /// <summary>
        /// Compares this MongoGridFSSettings to another one.
        /// </summary>
        /// <param name="rhs">The other MongoGridFSSettings.</param>
        /// <returns>True if the two settings are equal.</returns>
        public bool Equals(MongoGridFSSettings rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }
            return
                this.chunkSize == rhs.chunkSize &&
                this.root == rhs.root &&
                this.safeMode == rhs.safeMode;
        }

        /// <summary>
        /// Compares this MongoGridFSSettings to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other objects is a MongoGridFSSettings and is equal to this one.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as MongoGridFSSettings); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Freezes the settings.
        /// </summary>
        /// <returns>The frozen settings.</returns>
        public MongoGridFSSettings Freeze()
        {
            if (!isFrozen)
            {
                safeMode = safeMode.FrozenCopy();
                isFrozen = true;
            }
            return this;
        }

        /// <summary>
        /// Returns a frozen copy of the settings.
        /// </summary>
        /// <returns>A frozen copy of the settings.</returns>
        public MongoGridFSSettings FrozenCopy()
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
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + chunkSize.GetHashCode();
            hash = 37 * hash + root.GetHashCode();
            hash = 37 * hash + safeMode.GetHashCode();
            return hash;
        }

        // private methods
        private void ThrowFrozen()
        {
            throw new InvalidOperationException("A MongoGridFSSettings object cannot be modified once it has been frozen.");
        }
    }
}
