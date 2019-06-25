/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the void input data type to a pipeline with no input, such as a database aggregation. No actual instances of this type can be created.
    /// </summary>
    public sealed class NoPipelineInput
    {
        private NoPipelineInput()
        {
        }
    }

    /// <summary>
    /// The serializer for NoPipelineInput.
    /// </summary>
    internal sealed class NoPipelineInputSerializer : IBsonSerializer<NoPipelineInput>
    {
        #region static
        // private static fields
        private static readonly NoPipelineInputSerializer __instance = new NoPipelineInputSerializer();

        // public static properties
        /// <summary>
        ///  Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static NoPipelineInputSerializer Instance => __instance;
        #endregion

        /// <inheritdoc/>
        public Type ValueType => typeof(NoPipelineInput);


        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public NoPipelineInput Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            throw new NotSupportedException();
        }

        void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, NoPipelineInput value)
        {
            throw new NotSupportedException();
        }
    }
}
