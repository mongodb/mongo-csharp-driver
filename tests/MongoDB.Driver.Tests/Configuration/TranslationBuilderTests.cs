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

using FluentAssertions;
using MongoDB.Driver.Configuration;
using Xunit;

namespace MongoDB.Driver.Tests.Configuration;

public class TranslationBuilderTests
{
    [Fact]
    public void Properties_should_default_to_null()
    {
        var subject = new TranslationBuilder();

        subject.CompatibilityLevel.Should().BeNull();
        subject.EnableClientSideProjections.Should().NotHaveValue();
    }

    [Fact]
    public void Properties_should_be_settable_independently()
    {
        var subject = new TranslationBuilder();

        subject.CompatibilityLevel = ServerVersion.Server70;

        subject.CompatibilityLevel.Should().Be(ServerVersion.Server70);
        subject.EnableClientSideProjections.Should().NotHaveValue();

        subject.EnableClientSideProjections = false;

        subject.EnableClientSideProjections.Should().BeFalse();
    }

    [Fact]
    public void Properties_should_accept_null_after_being_set()
    {
        var subject = new TranslationBuilder
        {
            CompatibilityLevel = ServerVersion.Server70,
            EnableClientSideProjections = true
        };

        subject.CompatibilityLevel = null;
        subject.EnableClientSideProjections = null;

        subject.CompatibilityLevel.Should().BeNull();
        subject.EnableClientSideProjections.Should().NotHaveValue();
    }
}
