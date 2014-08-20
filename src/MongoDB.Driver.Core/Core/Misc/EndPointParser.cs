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
    public static class EndPointParser
    {
        // static methods
        public static EndPoint Parse(string value)
        {
            Ensure.IsNotNull(value, "value");

            EndPoint endPoint;
            if (!TryParse(value, out endPoint))
            {
                var message = string.Format("'{0}' is not a valid end point.", value);
                throw new ArgumentException(message, "value");
            }

            return endPoint;
        }

        public static bool TryParse(string value, out EndPoint endPoint)
        {
            endPoint = null;

            if (value != null)
            {
                var match = Regex.Match(value, @"^(?<address>\[[^]]+\])(:(?<port>\d+))?$");
                if (match.Success)
                {
                    var addressString = match.Groups["address"].Value;
                    var portString = match.Groups["port"].Value;
                    var port = portString.Length == 0 ? 27017 : int.Parse(portString, CultureInfo.InvariantCulture);

                    IPAddress address;
                    if (IPAddress.TryParse(addressString, out address))
                    {
                        endPoint = new IPEndPoint(address, port);
                        return true;
                    }

                    return false;
                }

                match = Regex.Match(value, @"^(?<host>[^:]+)(:(?<port>\d+))?$");
                if (match.Success)
                {
                    var host = match.Groups["host"].Value;
                    var portString = match.Groups["port"].Value;
                    var port = portString.Length == 0 ? 27017 : int.Parse(portString, CultureInfo.InvariantCulture);

                    IPAddress address;
                    if (IPAddress.TryParse(host, out address))
                    {
                        endPoint = new IPEndPoint(address, port);
                        return true;
                    }

                    try
                    {
                        endPoint = new DnsEndPoint(host, port);
                        return true;
                    }
                    catch (ArgumentException)
                    {
                        return false;
                    }
                }
            }

            return false;
        }
    }
}
