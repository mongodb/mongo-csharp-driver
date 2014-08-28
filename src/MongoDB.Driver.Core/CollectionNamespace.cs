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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver
{
    public sealed class CollectionNamespace : IEquatable<CollectionNamespace>
    {
        // static methods
        public static bool IsValid(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                return false;
            }

            var index = collectionName.IndexOf('\0');
            return index == -1;
        }

        public static implicit operator CollectionNamespace(string value)
        {
            return new CollectionNamespace(value);
        }

        public static implicit operator string(CollectionNamespace name)
        {
            return name.ToString();
        }

        // fields
        private readonly string _collectionName;
        private readonly DatabaseNamespace _databaseNamespace;

        // constructors
        public CollectionNamespace(string fullName)
        {
            Ensure.IsNotNull(fullName, "fullName");
            var index = fullName.IndexOf('.');
            if (index > -1)
            {
                _databaseNamespace = fullName.Substring(0, index);
                _collectionName = fullName.Substring(index + 1);
            }
            else
            {
                throw new ArgumentException("Must contain a '.' separating the database name from the collection name.", "fullName");
            }
        }
        
        public CollectionNamespace(DatabaseNamespace databaseNamespace, string collectionName)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, "databaseNamespace");
            _collectionName = Ensure.IsValid(collectionName, "collectionName", IsValid, "Collection names must be non-empty and not contain the null character.");
        }

        // properties
        public string CollectionName
        {
            get { return _collectionName; }
        }
        
        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        public string FullName
        {
            get { return _databaseNamespace.DatabaseName + "." + _collectionName; }
        }

        // methods
        public bool Equals(CollectionNamespace other)
        {
            if(other == null)
            {
                return false;
            }

            return _databaseNamespace.Equals(other._databaseNamespace) &&
                _collectionName == other._collectionName;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CollectionNamespace);
        }

        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_databaseNamespace)
                .Hash(_collectionName)
                .GetHashCode();
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}