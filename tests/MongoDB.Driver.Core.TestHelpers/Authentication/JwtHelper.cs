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
using System.IdentityModel.Tokens.Jwt;
using System.IO;

namespace MongoDB.Driver.Core.TestHelpers.Authentication
{
    public static class JwtHelper
    {
        public static (string Subject, DateTime? ExpirationDate) GetJwtDetails(string accessToken)
        {
            var jwtSecurityToken = new JwtSecurityToken(accessToken);
            return (jwtSecurityToken.Subject, jwtSecurityToken.ValidTo);
        }

        public static string GetTokenContent(string token = null)
        {
            if (token != null && !File.Exists(token))
            {
                return token;
            }

            var tokenFileName = token ??
                Path.GetDirectoryName(Environment.GetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE"));
            return tokenFileName != null
                ? File.ReadAllText(tokenFileName)
                : throw new Exception($"No jwt token file found: {tokenFileName}.");
        }

        public static string GetValidTokenOrThrow(string accessToken, DateTime? onDate = null)
        {
            onDate = onDate ?? DateTime.UtcNow;
            accessToken = File.Exists(accessToken) ? GetTokenContent(accessToken) : accessToken;
            var isValid = GetJwtDetails(accessToken).ExpirationDate > onDate;
            return isValid ? accessToken : throw new Exception($"The jwt token is expired on {onDate}.");
        }
    }
}
