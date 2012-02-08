﻿/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Bson
{
    /// <summary>
    /// A static helper class containing BSON defaults.
    /// </summary>
    public static class BsonDefaults
    {
        // private static fields
        private static GuidRepresentation __guidRepresentation = GuidRepresentation.CSharpLegacy;
        private static int __maxDocumentSize = 4 * 1024 * 1024; // 4MiB

        // public static properties
        /// <summary>
        /// Gets or sets the default representation to be used in serialization of 
        /// Guids to the database. 
        /// <seealso cref="MongoDB.Bson.GuidRepresentation"/> 
        /// </summary>
        public static GuidRepresentation GuidRepresentation
        {
            get { return __guidRepresentation; }
            set { __guidRepresentation = value; }
        }

        /// <summary>
        /// Gets or sets the default max document size. The default is 4MiB.
        /// </summary>
        public static int MaxDocumentSize
        {
            get { return __maxDocumentSize; }
            set { __maxDocumentSize = value; }
        }
    }
}
