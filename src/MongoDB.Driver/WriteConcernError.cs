/* Copyright 2010-present MongoDB Inc.
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
    /// Represents the details of a write concern error.
    /// </summary>
    public class WriteConcernError
    {
        // private fields
        private readonly int _code;
        private readonly string _codeName;
        private readonly BsonDocument _details;
        private readonly IEnumerable<string> _errorLabels;
        private readonly string _message;

        // constructors
        internal WriteConcernError(
            int code,
            string codeName,
            string message,
            BsonDocument details,
            IEnumerable<string> errorLabels)
        {
            _code = code;
            _codeName = codeName;
            _details = details;
            _errorLabels = errorLabels;
            _message = message;
        }

        // public properties
        /// <summary>
        /// Gets the error code.
        /// </summary>
        public int Code
        {
            get { return _code; }
        }

        /// <summary>
        /// Gets the name of the error code.
        /// </summary>
        /// <value>
        /// The name of the error code.
        /// </value>
        public string CodeName
        {
            get { return _codeName; }
        }

        /// <summary>
        /// Gets the error information.
        /// </summary>
        public BsonDocument Details
        {
            get { return _details; }
        }

        /// <summary>
        /// Gets the error labels.
        /// </summary>
        public IEnumerable<string> ErrorLabels
        {
            get { return _errorLabels; }
        }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message
        {
            get { return _message; }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder($"{{ Code : \"{_code}\"");
            if (_codeName != null)
            {
                sb.Append($", CodeName : \"{_codeName}\"");
            }
            if (_message != null)
            {
                sb.Append($", Message : \"{_message}\"");
            }
            if (_details != null)
            {
                sb.Append($", Details : \"{_details}\"");
            }
            if (_errorLabels != null && _errorLabels.Any())
            {
                sb.Append(", ErrorLabels : [ ");
                foreach (var errorLabel in _errorLabels)
                {
                    sb.Append($"\"{errorLabel}\", ");
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(" ]");
            }
            sb.Append(" }");

            return sb.ToString();
        }

        // internal static methods
        internal static WriteConcernError FromCore(Core.Operations.BulkWriteConcernError error)
        {
            return error == null ? null : new WriteConcernError(error.Code, error.CodeName, error.Message, error.Details, error.ErrorLabels);
        }
    }
}
