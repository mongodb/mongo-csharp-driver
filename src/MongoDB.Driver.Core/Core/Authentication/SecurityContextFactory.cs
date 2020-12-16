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

using System.Runtime.InteropServices;
using System.Security;
using MongoDB.Driver.Core.Authentication.Libgssapi;
using MongoDB.Driver.Core.Authentication.Sspi;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Authentication
{
    internal static class SecurityContextFactory
    {
        public static ISecurityContext InitializeSecurityContext(ConnectionId connectionId, string serviceName, string hostname, string realm, string authorizationId, SecureString password)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                catch (Win32Exception ex)
                {
                    credential?.Dispose();
                    throw new MongoAuthenticationException(connectionId, "Unable to acquire security credential.", ex);
                }
            }
            else
            {
                GssapiServicePrincipalName servicePrincipalName = null;
                GssapiSecurityCredential credential = null;
                try
                {
                    servicePrincipalName = GssapiServicePrincipalName.Create(serviceName, hostname, realm);
                    credential = GssapiSecurityCredential.Acquire(authorizationId, password);
                    return new GssapiSecurityContext(servicePrincipalName, credential);
                }
                catch (LibgssapiException ex)
                {
                    servicePrincipalName?.Dispose();
                    credential?.Dispose();
                    throw new MongoAuthenticationException(connectionId, "Unable to acquire security credential.", ex);
                }
            }
        }
    }
}
