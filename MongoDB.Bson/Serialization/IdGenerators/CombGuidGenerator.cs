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

namespace MongoDB.Bson.Serialization.IdGenerators
{
    /// <summary>
    /// Represents an Id generator for Guids using the COMB algorithm.
    /// </summary>
    public class CombGuidGenerator : IIdGenerator
    {
        // private static fields
        private static CombGuidGenerator __instance = new CombGuidGenerator();

        // constructors
        /// <summary>
        /// Initializes a new instance of the CombGuidGenerator class.
        /// </summary>
        public CombGuidGenerator()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of CombGuidGenerator.
        /// </summary>
        public static CombGuidGenerator Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Generates an Id for a document.
        /// </summary>
        /// <param name="container">The container of the document (will be a MongoCollection when called from the C# driver). </param>
        /// <param name="document">The document.</param>
        /// <returns>An Id.</returns>
        public object GenerateId(object container, object document)
        {
            var baseDate = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var now = DateTime.UtcNow;
            var days = (ushort)(now - baseDate).TotalDays;
            var milliseconds = (int)now.TimeOfDay.TotalMilliseconds;

            // replace last 6 bytes of a new Guid with 2 bytes from days and 4 bytes from milliseconds
            // see: The Cost of GUIDs as Primary Keys by Jimmy Nilson
            // at: http://www.informit.com/articles/article.aspx?p=25862&seqNum=7

            var bytes = Guid.NewGuid().ToByteArray();
            Array.Copy(BitConverter.GetBytes(days), 0, bytes, 10, 2);
            Array.Copy(BitConverter.GetBytes(milliseconds), 0, bytes, 12, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 10, 2);
                Array.Reverse(bytes, 12, 4);
            }
            return new Guid(bytes);
        }

        /// <summary>
        /// Tests whether an Id is empty.
        /// </summary>
        /// <param name="id">The Id.</param>
        /// <returns>True if the Id is empty.</returns>
        public bool IsEmpty(object id)
        {
            return id == null || (Guid)id == Guid.Empty;
        }
    }
}
