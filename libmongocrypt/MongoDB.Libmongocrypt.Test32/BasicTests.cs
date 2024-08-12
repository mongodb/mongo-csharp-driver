/*
 * Copyright 2019–present MongoDB, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Xunit;
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Libmongocrypt.Test32
{
    public class BasicTests
    {
        BsonDocument CreateAwsCredentialsDocument() =>
           new BsonDocument
               {
                    {
                        "aws",
                        new BsonDocument
                        {
                            { "secretAccessKey", "us-east-1" },
                            { "accessKeyId", "us-east-1" }
                        }
                    }
               };

        CryptOptions CreateOptions() =>
            new CryptOptions(
                new[]
                {
                    new KmsCredentials(CreateAwsCredentialsDocument().ToBson())
                }
            );

        [Fact]
        public void CryptClientShouldFailToiInitializeWhenTargetingX86()
        {
            var exception = Record.Exception(() => CryptClientFactory.Create(CreateOptions()));
            exception.Should().BeOfType<PlatformNotSupportedException>();
        }
    }
}
