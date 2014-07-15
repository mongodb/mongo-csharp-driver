/* Copyright 2013-2014 MongoDB Inc.
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
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Misc
{
    public static class DnsEndPointParser
    {
        // static methods
        public static DnsEndPoint Parse(string value, AddressFamily addressFamily)
        {
            Ensure.IsNotNull(value, "value");

            DnsEndPoint endPoint;
            if (!TryParse(value, addressFamily, out endPoint))
            {
                var message = string.Format("'{0}' is not a valid end point.", value);
                throw new ArgumentException(message, "value");
            }

            return endPoint;
        }

        public static string ToString(DnsEndPoint endPoint)
        {
            return string.Format("{0}:{1}", endPoint.Host, endPoint.Port);
        }

        public static bool TryParse(string value, AddressFamily addressFamily, out DnsEndPoint endPoint)
        {
            endPoint = null;

            if (value != null)
            {
                var match = Regex.Match(value, @"^(?<host>(\[[^]]+\]|[^:\[\]]+))(:(?<port>\d+))?$");
                if (match.Success)
                {
                    string host = match.Groups["host"].Value;
                    string portString = match.Groups["port"].Value;
                    int port = (portString != "") ? int.Parse(portString, CultureInfo.InvariantCulture) : 27017;
                    try
                    {
                        endPoint = new DnsEndPoint(host, port, addressFamily);
                    }
                    catch(ArgumentException)
                    {
                        endPoint = null;
                    }
                }
            }

            return endPoint != null;
        }
    }
}
