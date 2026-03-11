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

#if CSHARP_14

using FluentAssertions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization;

public class ExtensionMembersTests
{
    [Fact]
    public void Poco_with_extension_property_should_serialize_without_error()
    {
        var myPoco = new MyPoco { ExtensionProperty = "foo" };

        var json = myPoco.ToJson();

        json.Should().Be("{ \"SimpleProperty\" : \"foo\" }");
    }
}

public class MyPoco
{
    public string SimpleProperty { get; set; }
}

public static class MyPocoExtensions
{
// Until resolution of https://github.com/dotnet/sdk/issues/51681
#pragma warning disable CA1034
    extension(MyPoco myPoco)
#pragma warning restore CA1034
    {
        // Extension property:
        public string ExtensionProperty
        {
            get => myPoco.SimpleProperty + "_extended";
            set => myPoco.SimpleProperty = value;
        }
    }
}

#endif
