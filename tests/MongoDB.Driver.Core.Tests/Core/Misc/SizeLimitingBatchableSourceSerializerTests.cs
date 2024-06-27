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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations.ElementNameValidators;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Misc
{
    public class SizeLimitingBatchableSourceSerializerTests
    {
        private static readonly IBsonSerializer<int> __itemSerializer1 = new Int32Serializer(BsonType.Int32);
        private static readonly IBsonSerializer<int> __itemSerializer2 = new Int32Serializer(BsonType.String);
        private static IElementNameValidator __itemElementNameValidator1 = new NoOpElementNameValidator();
        private static IElementNameValidator __itemElementNameValidator2 = new UpdateElementNameValidator();
        private static int __maxBatchCount1 = 1;
        private static int __maxBatchCount2 = 2;
        private static int __maxBatchSize1 = 1;
        private static int __maxBatchSize2 = 2;
        private static int __maxItemSize1 = 1;
        private static int __maxItemSize2 = 2;

        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new SizeLimitingBatchableSourceSerializer<int>(__itemSerializer1, __itemElementNameValidator1, __maxBatchCount1, __maxItemSize1, __maxBatchSize1);
            var y = new DerivedFromSizeLimitingBatchableSourceSerializer<int>(__itemSerializer1, __itemElementNameValidator1, __maxBatchCount1, __maxItemSize1, __maxBatchSize1);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new SizeLimitingBatchableSourceSerializer<int>(__itemSerializer1, __itemElementNameValidator1, __maxBatchCount1, __maxItemSize1, __maxBatchSize1);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new SizeLimitingBatchableSourceSerializer<int>(__itemSerializer1, __itemElementNameValidator1, __maxBatchCount1, __maxItemSize1, __maxBatchSize1);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new SizeLimitingBatchableSourceSerializer<int>(__itemSerializer1, __itemElementNameValidator1, __maxBatchCount1, __maxItemSize1, __maxBatchSize1);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new SizeLimitingBatchableSourceSerializer<int>(__itemSerializer1, __itemElementNameValidator1, __maxBatchCount1, __maxItemSize1, __maxBatchSize1);
            var y = new SizeLimitingBatchableSourceSerializer<int>(__itemSerializer1, __itemElementNameValidator1, __maxBatchCount1, __maxItemSize1, __maxBatchSize1);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("itemElementNameValidator")]
        [InlineData("itemSerializer")]
        [InlineData("maxBatchCount")]
        [InlineData("maxBatchSize")]
        [InlineData("maxItemSize")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var x = new SizeLimitingBatchableSourceSerializer<int>(__itemSerializer1, __itemElementNameValidator1, __maxBatchCount1, __maxItemSize1, __maxBatchSize1);
            var y = notEqualFieldName switch
            {
                "itemSerializer" => new SizeLimitingBatchableSourceSerializer<int>(__itemSerializer2, __itemElementNameValidator1, __maxBatchCount1, __maxItemSize1, __maxBatchSize1),
                "itemElementNameValidator" => new SizeLimitingBatchableSourceSerializer<int>(__itemSerializer1, __itemElementNameValidator2, __maxBatchCount1, __maxItemSize1, __maxBatchSize1),
                "maxBatchCount" => new SizeLimitingBatchableSourceSerializer<int>(__itemSerializer1, __itemElementNameValidator1, __maxBatchCount2, __maxItemSize1, __maxBatchSize1),
                "maxBatchSize" => new SizeLimitingBatchableSourceSerializer<int>(__itemSerializer1, __itemElementNameValidator1, __maxBatchCount1, __maxItemSize2, __maxBatchSize1),
                "maxItemSize" => new SizeLimitingBatchableSourceSerializer<int>(__itemSerializer1, __itemElementNameValidator1, __maxBatchCount1, __maxItemSize1, __maxBatchSize2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new SizeLimitingBatchableSourceSerializer<int>(__itemSerializer1, __itemElementNameValidator1, __maxBatchCount1, __maxItemSize1, __maxBatchSize1);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class DerivedFromSizeLimitingBatchableSourceSerializer<TItem> : SizeLimitingBatchableSourceSerializer<TItem>
        {
            public DerivedFromSizeLimitingBatchableSourceSerializer(IBsonSerializer<TItem> itemSerializer, IElementNameValidator itemElementNameValidator, int maxBatchCount, int maxItemSize, int maxBatchSize)
                : base(itemSerializer, itemElementNameValidator, maxBatchCount, maxItemSize, maxBatchSize)
            {
            }
        }
    }
}
