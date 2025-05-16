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
using FluentAssertions;
using Xunit;

namespace MongoDB.Bson.Tests.ObjectModel
{
    public class BsonTypeMapperOptionsTests
    {
        [Fact]
        public void Clone_should_create_independent_copy_with_same_values()
        {
            // Arrange
            var original = new BsonTypeMapperOptions
            {
                DuplicateNameHandling = DuplicateNameHandling.ThrowException,
                MapBsonArrayTo = typeof(object[]),
                MapBsonDocumentTo = typeof(Dictionary<string, string>),
                MapOldBinaryToByteArray = true
            };

            // Act
            var clone = original.Clone();

            // Assert
            clone.Should().NotBeSameAs(original);
            clone.DuplicateNameHandling.Should().Be(original.DuplicateNameHandling);
            clone.MapBsonArrayTo.Should().Be(original.MapBsonArrayTo);
            clone.MapBsonDocumentTo.Should().Be(original.MapBsonDocumentTo);
            clone.MapOldBinaryToByteArray.Should().Be(original.MapOldBinaryToByteArray);
            clone.IsFrozen.Should().BeFalse();
        }

        [Fact]
        public void Constructor_should_initialize_default_values()
        {
            // Act
            var options = new BsonTypeMapperOptions();

            // Assert
            options.DuplicateNameHandling.Should().Be(DuplicateNameHandling.Overwrite);
            options.MapBsonArrayTo.Should().Be(typeof(List<object>));
            options.MapBsonDocumentTo.Should().Be(typeof(Dictionary<string, object>));
            options.MapOldBinaryToByteArray.Should().BeFalse();
            options.IsFrozen.Should().BeFalse();
        }

        [Fact]
        public void Defaults_get_should_return_default_options_instance()
        {
            // Act
            var defaults = BsonTypeMapperOptions.Defaults;

            // Assert
            defaults.Should().NotBeNull();
            defaults.IsFrozen.Should().BeTrue();
        }

        [Fact]
        public void Defaults_set_should_freeze_clone_when_value_is_not_frozen()
        {
            // Arrange
            var originalDefaults = BsonTypeMapperOptions.Defaults;
            var newOptions = new BsonTypeMapperOptions
            {
                DuplicateNameHandling = DuplicateNameHandling.ThrowException
            };

            try
            {
                // Act
                BsonTypeMapperOptions.Defaults = newOptions;

                // Assert
                BsonTypeMapperOptions.Defaults.Should().NotBeSameAs(newOptions);
                BsonTypeMapperOptions.Defaults.DuplicateNameHandling.Should().Be(DuplicateNameHandling.ThrowException);
                BsonTypeMapperOptions.Defaults.IsFrozen.Should().BeTrue();
            }
            finally
            {
                // Restore original defaults
                BsonTypeMapperOptions.Defaults = originalDefaults;
            }
        }

        [Fact]
        public void Defaults_set_should_use_frozen_instance_when_value_is_frozen()
        {
            // Arrange
            var originalDefaults = BsonTypeMapperOptions.Defaults;
            var newFrozenOptions = new BsonTypeMapperOptions
            {
                DuplicateNameHandling = DuplicateNameHandling.ThrowException
            }.Freeze();

            try
            {
                // Act
                BsonTypeMapperOptions.Defaults = newFrozenOptions;

                // Assert
                BsonTypeMapperOptions.Defaults.Should().BeSameAs(newFrozenOptions);
                BsonTypeMapperOptions.Defaults.DuplicateNameHandling.Should().Be(DuplicateNameHandling.ThrowException);
            }
            finally
            {
                // Restore original defaults
                BsonTypeMapperOptions.Defaults = originalDefaults;
            }
        }

        [Fact]
        public void DuplicateNameHandling_get_should_return_set_value()
        {
            // Arrange
            var options = new BsonTypeMapperOptions
            {
                DuplicateNameHandling = DuplicateNameHandling.ThrowException
            };

            // Act
            var result = options.DuplicateNameHandling;

            // Assert
            result.Should().Be(DuplicateNameHandling.ThrowException);
        }

        [Fact]
        public void DuplicateNameHandling_set_should_throw_when_instance_is_frozen()
        {
            // Arrange
            var options = new BsonTypeMapperOptions().Freeze();

            // Act
            Action act = () => options.DuplicateNameHandling = DuplicateNameHandling.ThrowException;

            // Assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("BsonTypeMapperOptions is frozen.");
        }

        [Fact]
        public void Freeze_should_be_idempotent()
        {
            // Arrange
            var options = new BsonTypeMapperOptions();
            options.Freeze();

            // Act
            var result = options.Freeze();

            // Assert
            result.Should().BeSameAs(options);
            result.IsFrozen.Should().BeTrue();
        }

        [Fact]
        public void Freeze_should_set_IsFrozen_to_true()
        {
            // Arrange
            var options = new BsonTypeMapperOptions();

            // Act
            var result = options.Freeze();

            // Assert
            result.Should().BeSameAs(options);
            result.IsFrozen.Should().BeTrue();
        }

        [Fact]
        public void IsFrozen_get_should_return_false_by_default()
        {
            // Arrange
            var options = new BsonTypeMapperOptions();

            // Act
            var result = options.IsFrozen;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void MapBsonArrayTo_get_should_return_set_value()
        {
            // Arrange
            var options = new BsonTypeMapperOptions
            {
                MapBsonArrayTo = typeof(object[])
            };

            // Act
            var result = options.MapBsonArrayTo;

            // Assert
            result.Should().Be(typeof(object[]));
        }

        [Fact]
        public void MapBsonArrayTo_set_should_throw_when_instance_is_frozen()
        {
            // Arrange
            var options = new BsonTypeMapperOptions().Freeze();

            // Act
            Action act = () => options.MapBsonArrayTo = typeof(object[]);

            // Assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("BsonTypeMapperOptions is frozen.");
        }

        [Fact]
        public void MapBsonDocumentTo_get_should_return_set_value()
        {
            // Arrange
            var options = new BsonTypeMapperOptions
            {
                MapBsonDocumentTo = typeof(Dictionary<string, string>)
            };

            // Act
            var result = options.MapBsonDocumentTo;

            // Assert
            result.Should().Be(typeof(Dictionary<string, string>));
        }

        [Fact]
        public void MapBsonDocumentTo_set_should_throw_when_instance_is_frozen()
        {
            // Arrange
            var options = new BsonTypeMapperOptions().Freeze();

            // Act
            Action act = () => options.MapBsonDocumentTo = typeof(Dictionary<string, string>);

            // Assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("BsonTypeMapperOptions is frozen.");
        }

        [Fact]
        public void MapOldBinaryToByteArray_get_should_return_set_value()
        {
            // Arrange
            var options = new BsonTypeMapperOptions
            {
                MapOldBinaryToByteArray = true
            };

            // Act
            var result = options.MapOldBinaryToByteArray;

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void MapOldBinaryToByteArray_set_should_throw_when_instance_is_frozen()
        {
            // Arrange
            var options = new BsonTypeMapperOptions().Freeze();

            // Act
            Action act = () => options.MapOldBinaryToByteArray = true;

            // Assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("BsonTypeMapperOptions is frozen.");
        }
    }
}
