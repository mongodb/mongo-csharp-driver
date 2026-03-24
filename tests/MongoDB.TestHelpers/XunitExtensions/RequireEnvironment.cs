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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Xunit.Sdk;

namespace MongoDB.TestHelpers.XunitExtensions
{
    public class RequireEnvironment
    {
        #region static
        public static RequireEnvironment Check()
        {
            return new RequireEnvironment();
        }
        #endregion

        public RequireEnvironment EnvironmentVariable(string name, bool isDefined = true, bool allowEmpty = true)
        {
            var actualValue = Environment.GetEnvironmentVariable(name);
            var actualIsDefined = actualValue != null;
            if (actualIsDefined == isDefined && (allowEmpty || !string.IsNullOrEmpty(actualValue)))
            {
                return this;
            }
            throw new SkipException($"Test skipped because environment variable '{name}' {(actualIsDefined ? "is" : "is not")} defined.");
        }

        public RequireEnvironment EnvironmentVariable(string name, params string[] matchValues)
        {
            var actualValue = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(actualValue))
            {
                throw new SkipException($"Test skipped because environment variable '{name}' is not defined.");
            }
            if (matchValues.Contains(actualValue))
            {
                return this;
            }
            throw new SkipException($"Test skipped because environment variable '{name}'={actualValue} does not satisfy expected values.");
        }

        public RequireEnvironment ProcessStarted(string processName)
        {
            if (Process.GetProcessesByName(processName).Length > 0)
            {
                return this;
            }
            throw new SkipException($"Test skipped because an OS process {processName} has not been detected.");
        }

        public RequireEnvironment RequireKmsProviders(string names)
        {
            foreach (var name in names.Split([";"], StringSplitOptions.None))
            {
                switch (name)
                {
                    case "aws":
                    case "aws:name1":
                        EnvironmentVariable("FLE_AWS_KEY");
                        EnvironmentVariable("FLE_AWS_SECRET");
                        break;
                    case "aws:name2":
                        EnvironmentVariable("FLE_AWS_KEY2");
                        EnvironmentVariable("FLE_AWS_SECRET2");
                        break;
                    case "azure":
                    case "azure:name1":
                        EnvironmentVariable("FLE_AZURE_TENANTID");
                        EnvironmentVariable("FLE_AZURE_CLIENTID");
                        EnvironmentVariable("FLE_AZURE_CLIENTSECRET");
                        break;
                    case "gcp":
                    case "gcp:name1":
                        EnvironmentVariable("FLE_GCP_EMAIL");
                        EnvironmentVariable("FLE_GCP_PRIVATEKEY");
                        break;
                    case "awsTemporary":
                        EnvironmentVariable("CSFLE_AWS_TEMP_SESSION_TOKEN");
                        goto case "awsTemporaryNoSessionToken";
                    case "awsTemporaryNoSessionToken":
                        EnvironmentVariable("CSFLE_AWS_TEMP_ACCESS_KEY_ID");
                        EnvironmentVariable("CSFLE_AWS_TEMP_SECRET_ACCESS_KEY");
                        break;
                    case "local":
                    case "local:name1":
                    case "local:name2":
                    case "kmip":
                    case "kmip:name1":
                        break;
                    default:
                        throw new NotImplementedException($"KMS provider {name} is not not known.");
                }
            }

            return this;
        }


        public RequireEnvironment HostReachable(DnsEndPoint endPoint)
        {
            if (IsReachable())
            {
                return this;
            }
            throw new SkipException($"Test skipped because expected server {endPoint} is not reachable.");

            bool IsReachable()
            {
                using (TcpClient tcpClient = new TcpClient())
                {
                    try
                    {
                        tcpClient.Connect(endPoint.Host, endPoint.Port);
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }
        }
    }
}
