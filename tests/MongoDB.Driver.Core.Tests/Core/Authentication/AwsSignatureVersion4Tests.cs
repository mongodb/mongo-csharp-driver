/* Copyright 2020-present MongoDB Inc.
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
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Shared;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Authentication
{
    public class AwsSignatureVersion4Tests
    {
        [Fact]
        public void CreateAuthorizationRequest_should_have_expected_result()
        {
            var date = new DateTime(2020, 03, 12, 14, 23, 46);
            var accessKeyId = "permanentuser";
            var secretAccessKey = "FAKEFAKEFAKEFAKEFAKEfakefakefakefakefake";
            var salt = new byte[] { 64, 230, 20, 164, 223, 96, 92, 144, 3, 240, 27, 110, 97, 65, 200, 11, 157, 162, 141, 4, 149, 86, 91, 108, 189, 194, 100, 90, 249, 219, 155, 235, };
            var host = "sts.amazonaws.com";
            var expectedAuthorizationHeader = "AWS4-HMAC-SHA256 " +
                "Credential=permanentuser/20200312/us-east-1/sts/aws4_request, " +
                "SignedHeaders=content-length;content-type;host;x-amz-date;x-mongodb-gs2-cb-flag;x-mongodb-server-nonce, " +
                "Signature=6872b9199b47dc983a95f9113a096c9b4e63bb6ddf39030161b1f092ab616df2";
            var expectedTimestamp = "20200312T142346Z";

            AwsSignatureVersion4.CreateAuthorizationRequest(
                date,
                accessKeyId,
                SecureStringHelper.ToSecureString(secretAccessKey),
                sessionToken: null,
                salt,
                host,
                out var actualAuthorizationHeader,
                out var actualTimestamp);

            actualAuthorizationHeader.Should().Be(expectedAuthorizationHeader);
            actualTimestamp.Should().Be(expectedTimestamp);
        }

        [Fact]
        public void CreateAuthorizationRequest_with_session_token_should_have_expected_result()
        {
            var date = new DateTime(2020, 03, 12, 14, 23, 46);
            var accessKeyId = "permanentuser";
            var secretAccessKey = "FAKEFAKEFAKEFAKEFAKEfakefakefakefakefake";
            var sessionToken = "MXUpbuzwzPo67WKCNYtdBq47taFtIpt+SVx58hNx1/jSz37h9d67dtUOg0ejKrv83u8ai+VFZxMx=";
            var salt = new byte[] { 64, 230, 20, 164, 223, 96, 92, 144, 3, 240, 27, 110, 97, 65, 200, 11, 157, 162, 141, 4, 149, 86, 91, 108, 189, 194, 100, 90, 249, 219, 155, 235, };
            var host = "sts.amazonaws.com";
            var expectedAuthorizationHeader = "AWS4-HMAC-SHA256 " +
                "Credential=permanentuser/20200312/us-east-1/sts/aws4_request, " +
                "SignedHeaders=content-length;content-type;host;x-amz-date;x-amz-security-token;x-mongodb-gs2-cb-flag;x-mongodb-server-nonce, " +
                "Signature=d60ee7fe01c82631583a7534fe017e1840fd5975faf1593252e91c54573a93ae";
            var expectedTimestamp = "20200312T142346Z";

            AwsSignatureVersion4.CreateAuthorizationRequest(
                date,
                accessKeyId,
                SecureStringHelper.ToSecureString(secretAccessKey),
                sessionToken,
                salt,
                host,
                out var actualAuthorizationHeader,
                out var actualTimestamp);

            actualAuthorizationHeader.Should().Be(expectedAuthorizationHeader);
            actualTimestamp.Should().Be(expectedTimestamp);
        }

        [Fact]
        public void GetCanonicalHeaders_should_return_expected_result()
        {
            var timestamp = new DateTime(2020, 03, 12, 14, 23, 46).ToString("yyyyMMddTHHmmssZ");
            var requestHeaders = new SortedDictionary<string, string>
            {
                ["X-MongoDB-GS2-CB-Flag"] = "n",
                ["Content-Type"] = "application/x-www-form-urlencoded",
                ["X-Amz-Date"] = timestamp,
                ["Content-Length"] = "42",
                ["Host"] = "iam.testhost.com",
                ["X-MongoDB-Server-Nonce"] = "123",
                ["X-Amz-Security-Token"] = "321"
            };
            var expected = "content-length:42\n" +
                "content-type:application/x-www-form-urlencoded\n" +
                "host:iam.testhost.com\n" +
                "x-amz-date:20200312T142346Z\n" +
                "x-amz-security-token:321\n" +
                "x-mongodb-gs2-cb-flag:n\n" +
                "x-mongodb-server-nonce:123";

            var actual = AwsSignatureVersion4Reflector.GetCanonicalHeaders(requestHeaders);

            actual.Should().Be(expected);
        }

        [Theory]
        [InlineData("sts.amazonaws.com", "us-east-1")]
        [InlineData("first", "us-east-1")]
        [InlineData("first.second", "second")]
        [InlineData("first.second.third", "second")]
        public void GetRegion_should_return_expected_result(string host, string expectedRegion)
        {
            var region = AwsSignatureVersion4Reflector.GetRegion(host);

            region.Should().Be(expectedRegion);
        }

        [Fact]
        public void GetSignedHeaders_should_return_expected_result()
        {
            var timestamp = new DateTime(2020, 03, 12, 14, 23, 46).ToString("yyyyMMddTHHmmssZ");
            var requestHeaders = new SortedDictionary<string, string>
            {
                ["X-Amz-Security-Token"] = "321",
                ["Content-Type"] = "application/x-www-form-urlencoded",
                ["X-Amz-Date"] = timestamp,
                ["Content-Length"] = "42",
                ["Host"] = "iam.testhost.com",
                ["X-MongoDB-GS2-CB-Flag"] = "n",
                ["X-MongoDB-Server-Nonce"] = "123"
            };
            var expected = "content-length;content-type;host;x-amz-date;x-amz-security-token;x-mongodb-gs2-cb-flag;x-mongodb-server-nonce";

            var actual = AwsSignatureVersion4Reflector.GetSignedHeaders(requestHeaders);

            actual.Should().Be(expected);
        }
    }

    internal static class AwsSignatureVersion4Reflector
    {
        public static string GetCanonicalHeaders(SortedDictionary<string, string> requestHeaders)
        {
            return (string)Reflector.InvokeStatic(typeof(AwsSignatureVersion4), nameof(GetCanonicalHeaders), requestHeaders);
        }

        public static string GetRegion(string host)
        {
            return (string)Reflector.InvokeStatic(typeof(AwsSignatureVersion4), nameof(GetRegion), host);
        }

        public static string GetSignedHeaders(SortedDictionary<string, string> requestHeaders)
        {
            return (string)Reflector.InvokeStatic(typeof(AwsSignatureVersion4), nameof(GetSignedHeaders), requestHeaders);
        }
    }
}
