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
using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a container for the CanCommandBeSentToSecondary delegate.
    /// </summary>
    public static class CanCommandBeSentToSecondary
    {
        // private static fields
        private static Func<BsonDocument, bool> __delegate = DefaultImplementation;
        private static HashSet<string> __secondaryOkCommands = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            "group",
            "aggregate",
            "collStats",
            "dbStats",
            "count",
            "distinct",
            "geoNear",
            "geoSearch",
            "geoWalk"
        };

        // public static properties
        /// <summary>
        /// Gets or sets the CanCommandBeSentToSecondary delegate.
        /// </summary>
        public static Func<BsonDocument, bool> Delegate
        {
            get { return __delegate; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                __delegate = value;
            }
        }

        // public static methods
        /// <summary>
        /// Default implementation of the CanCommandBeSentToSecondary delegate.
        /// </summary>
        /// <param name="document">The command.</param>
        /// <returns>True if the command can be sent to a secondary member of a replica set.</returns>
        public static bool DefaultImplementation(BsonDocument document)
        {
            if (document.ElementCount == 0)
            {
                return false;
            }

            var commandName = document.GetElement(0).Name;

            if (__secondaryOkCommands.Contains(commandName))
            {
                return true;
            }

            if (commandName.Equals("mapreduce", StringComparison.InvariantCultureIgnoreCase))
            {
                BsonValue outValue;
                if (document.TryGetValue("out", out outValue) && outValue.IsBsonDocument)
                {
                    return outValue.AsBsonDocument.Contains("inline");
                }
            }

            return false;
        }
    }
}
