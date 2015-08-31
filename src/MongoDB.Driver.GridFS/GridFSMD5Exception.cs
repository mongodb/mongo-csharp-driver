﻿/* Copyright 2015 MongoDB Inc.
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
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a GridFSMD5 exception.
    /// </summary>
    [Serializable]
    public class GridFSMD5Exception : GridFSException
    {
        #region static
        private static string FormatMessage(BsonValue id)
        {
            Ensure.IsNotNull(id, "id");
            return string.Format("GridFS MD5 check failed: file id {0}.", id);
        }
        #endregion

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GridFSMD5Exception"/> class.
        /// </summary>
        /// <param name="id">The file id.</param>
        public GridFSMD5Exception(BsonValue id)
            : base(FormatMessage(id))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridFSMD5Exception"/> class.
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        public GridFSMD5Exception(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
