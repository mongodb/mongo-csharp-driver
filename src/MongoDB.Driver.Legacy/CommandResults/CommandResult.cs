/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the result of a command (there are also subclasses for various commands).
    /// </summary>
#if NET45
    [Serializable]
#endif
    [BsonSerializer(typeof(CommandResultSerializer<CommandResult>))]
    public class CommandResult
    {
        // private fields
        private BsonDocument _response;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandResult"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        public CommandResult(BsonDocument response)
        {
            _response = response;
        }

        // public properties
        /// <summary>
        /// Gets the code.
        /// </summary>
        public int? Code
        {
            get { return _response.GetValue("code", BsonNull.Value).AsNullableInt32; }
        }

        /// <summary>
        /// Gets the response.
        /// </summary>
        public BsonDocument Response
        {
            get { return _response; }
        }

        /// <summary>
        /// Gets the error message (null if none).
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                BsonValue ok;
                if (_response.TryGetValue("ok", out ok) && ok.ToBoolean())
                {
                    return null;
                }
                else
                {
                    BsonValue errmsg;
                    if (_response.TryGetValue("errmsg", out errmsg) && !errmsg.IsBsonNull)
                    {
                        return errmsg.ToString();
                    }
                    else
                    {
                        return "Unknown error";
                    }
                }
            }
        }

        /// <summary>
        /// Gets the Ok value from the response.
        /// </summary>
        public bool Ok
        {
            get
            {
                BsonValue ok;
                if (_response.TryGetValue("ok", out ok))
                {
                    return ok.ToBoolean();
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
