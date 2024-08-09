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
using System.Security;
using MongoDB.Driver.Authentication.Gssapi.Libgssapi;
using MongoDB.Driver.Authentication.Gssapi.Sspi;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication.Gssapi
{
    internal static class SecurityContextFactory
    {
        public static ISecurityContext InitializeSecurityContext(string serviceName, string hostname, string realm, string authorizationId, SecureString password)
        {
            var operatingSystemPlatform = OperatingSystemHelper.CurrentOperatingSystem;
            switch (operatingSystemPlatform)
            {
                case OperatingSystemPlatform.Windows:
                {
                    SspiSecurityCredential credential = null;
                    try
                    {
                        var servicePrincipalName = $"{serviceName}/{hostname}";
                        if (!string.IsNullOrEmpty(realm))
                        {
                            servicePrincipalName += $"@{realm}";
                        }
                        credential = SspiSecurityCredential.Acquire(SspiPackage.Kerberos, authorizationId, password);
                        return new SspiSecurityContext(servicePrincipalName, credential);
                    }
                    catch (Win32Exception)
                    {
                        credential?.Dispose();
                        throw;
                    }
                }
                case OperatingSystemPlatform.Linux:
                {
                    GssapiServicePrincipalName servicePrincipalName = null;
                    GssapiSecurityCredential credential = null;
                    try
                    {
                        servicePrincipalName = GssapiServicePrincipalName.Create(serviceName, hostname, realm);
                        credential = GssapiSecurityCredential.Acquire(authorizationId, password);
                        return new GssapiSecurityContext(servicePrincipalName, credential);
                    }
                    catch (LibgssapiException)
                    {
                        servicePrincipalName?.Dispose();
                        credential?.Dispose();
                        throw;
                    }
                }
                default:
                    throw new NotSupportedException($"GSSAPI is not supported on {operatingSystemPlatform}.");
            }
        }
    }
}
