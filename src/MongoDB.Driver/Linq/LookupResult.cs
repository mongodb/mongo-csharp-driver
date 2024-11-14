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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// The result of a Lookup operation.
    /// </summary>
    /// <typeparam name="TLocal">The type of the local documents.</typeparam>
    /// <typeparam name="TResult">The type of the result values.</typeparam>
    [BsonSerializer(typeof(LookupResultSerializer<,>))]
    public struct LookupResult<TLocal, TResult>
    {
        /// <summary>
        /// The local document.
        /// </summary>
        public TLocal Local { get; init; }

        /// <summary>
        /// The result values.
        /// </summary>
        /// <notes>The result values are either the matching foreign documents themselves or the output of the pipeline that was run on the matching foreign documents.</notes>
        public TResult[] Results { get; init; }
    }

    internal static class LookupResultSerializer
    {
        public static IBsonSerializer Create(IBsonSerializer localSerializer, IBsonSerializer resultSerializer)
        {
            var localType = localSerializer.ValueType;
            var resultType = resultSerializer.ValueType;
            var lookupResultSerializerType = typeof(LookupResultSerializer<,>).MakeGenericType(localType, resultType);
            return (IBsonSerializer)Activator.CreateInstance(lookupResultSerializerType, localSerializer, resultSerializer);
        }
    }

    internal class LookupResultSerializer<TLocal, TResult> : StructSerializerBase<LookupResult<TLocal, TResult>>, IBsonDocumentSerializer
    {
        private readonly IBsonSerializer<TLocal> _localSerializer;
        private readonly IBsonSerializer<TResult[]> _resultsSerializer;

        public LookupResultSerializer(
            IBsonSerializer<TLocal> localSerializer,
            IBsonSerializer<TResult> resultSerializer)
        {
            _localSerializer = Ensure.IsNotNull(localSerializer, nameof(localSerializer));
             Ensure.IsNotNull(resultSerializer, nameof(resultSerializer));

             var arraySerializerType = typeof(ArraySerializer<>).MakeGenericType(resultSerializer.ValueType);
             _resultsSerializer = (IBsonSerializer<TResult[]>)Activator.CreateInstance(arraySerializerType, resultSerializer);
        }

        public override LookupResult<TLocal, TResult> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            reader.ReadStartDocument();
            reader.ReadName("_local");
            var local = _localSerializer.Deserialize(context);
            reader.ReadName("_results");
            var results = _resultsSerializer.Deserialize(context);
            reader.ReadEndDocument();

            return new LookupResult<TLocal, TResult> { Local = local, Results = results };
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = memberName switch
            {
                "Local" => new BsonSerializationInfo("_local", _localSerializer, typeof(TLocal)),
                "Results" => new BsonSerializationInfo("_results", _resultsSerializer, typeof(TResult)),
                _ => throw new ArgumentException($"{memberName} is not a member of LookupResult", nameof(memberName))
            };
            return serializationInfo != null;
        }
    }
}
