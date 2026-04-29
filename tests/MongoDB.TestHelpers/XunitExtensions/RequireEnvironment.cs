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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

        // Cloudflare WARP (and similar VPN software) can write both plain IPv4 and IPv4-mapped IPv6
        // forms of the same loopback address into /etc/resolv.conf (e.g. 127.0.2.3 and ::ffff:127.0.2.3).
        // DnsClient reads /etc/resolv.conf directly and may select the ::ffff: form for UDP queries,
        // which macOS does not route correctly, causing a socket timeout. Skip DNS-dependent tests
        // when this configuration is detected.
        public RequireEnvironment NoDuplicateIpv4MappedNameServers()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return this;
            }

            const string resolveConfPath = "/etc/resolv.conf";
            if (!File.Exists(resolveConfPath))
            {
                return this;
            }

            var ipv4Addresses = new HashSet<IPAddress>();
            var allAddresses = new List<IPAddress>();

            foreach (var line in File.ReadAllLines(resolveConfPath))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("nameserver", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && IPAddress.TryParse(parts[1], out var ip))
                {
                    allAddresses.Add(ip);
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipv4Addresses.Add(ip);
                    }
                }
            }

            if (allAddresses.Any(ip => ip.IsIPv4MappedToIPv6 && ipv4Addresses.Contains(ip.MapToIPv4())))
            {
                throw new SkipException(
                    "Test skipped because /etc/resolv.conf contains both an IPv4 nameserver address and its IPv4-mapped " +
                    "IPv6 equivalent (e.g. Cloudflare WARP). DnsClient may time out when it selects the ::ffff: form " +
                    "for a UDP query on this platform. See CSHARP-5930.");
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
