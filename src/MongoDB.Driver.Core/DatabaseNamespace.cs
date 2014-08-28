/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    public sealed class DatabaseNamespace : IEquatable<DatabaseNamespace>
    {
        // static fields
        private static readonly DatabaseNamespace __admin = new DatabaseNamespace("admin");

        // static properties
        public static DatabaseNamespace Admin
        {
            get { return __admin; }
        }

        // static methods
        public static bool IsValid(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var index = name.IndexOfAny(new[] { '\0', '.' });
            return index == -1;
        }

        // fields
        private readonly string _databaseName;

        // constructors
        public DatabaseNamespace(string databaseName)
        {
            Ensure.IsNotNull(databaseName, "databaseName");
            _databaseName = Ensure.That(databaseName, IsValid, "databaseName", "Database names must be non-empty and not contain '.' or the null character.");
        }

        // properties
        internal CollectionNamespace CommandCollection
        {
            get { return new CollectionNamespace(this, "$cmd"); }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        internal CollectionNamespace SystemIndexesCollection
        {
            get { return new CollectionNamespace(this, "system.indexes"); }
        }

        internal CollectionNamespace SystemNamespacesCollection
        {
            get { return new CollectionNamespace(this, "system.namespaces"); }
        }

        // methods
        public bool Equals(DatabaseNamespace other)
        {
            if(other == null)
            {
                return false;
            }

            return _databaseName == other._databaseName;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DatabaseNamespace);
        }

        public override int GetHashCode()
        {
            return _databaseName.GetHashCode();
        }

        public override string ToString()
        {
            return _databaseName;
        }
    }
}