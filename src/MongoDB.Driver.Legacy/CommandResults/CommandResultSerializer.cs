/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a serializer for a CommandResult.
    /// </summary>
    /// <typeparam name="TCommandResult">The type of the command result.</typeparam>
    public class CommandResultSerializer<TCommandResult> : SerializerBase<TCommandResult> where TCommandResult : CommandResult
    {
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <param name="args">The deserialization args.</param>
        /// <returns>The value.</returns>
        public override TCommandResult Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var response = BsonDocumentSerializer.Instance.Deserialize(context);
            return (TCommandResult)Activator.CreateInstance(typeof(TCommandResult), new object[] { response });
        }
    }
}
