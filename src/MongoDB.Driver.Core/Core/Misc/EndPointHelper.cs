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
    public static class EndPointHelper
    {
        // static fields
        private static IEqualityComparer<EndPoint> __endPointEqualityComparer = new EndPointEqualityComparerImpl();

        // static properties
        public static IEqualityComparer<EndPoint> EndPointEqualityComparer
        {
            get { return __endPointEqualityComparer; }
        }

        // static methods
        public static bool Contains(IEnumerable<EndPoint> endPoints, EndPoint endPoint)
        {
            return endPoints.Contains(endPoint, __endPointEqualityComparer);
        }

        public static bool Equals(EndPoint a, EndPoint b)
        {
            return __endPointEqualityComparer.Equals(a, b);
        }

        public static bool SequenceEquals(IEnumerable<EndPoint> a, IEnumerable<EndPoint> b)
        {
            return a.SequenceEqual(b, __endPointEqualityComparer);
        }

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

        public static string ToString(EndPoint endPoint)
        {
            var dnsEndPoint = endPoint as DnsEndPoint;
            if (dnsEndPoint != null)
            {
                return string.Format("{0}:{1}", dnsEndPoint.Host, dnsEndPoint.Port);
            }

            return endPoint.ToString();
        }

        public static bool TryParse(string value, out EndPoint endPoint)
        {
            endPoint = null;

            if (value != null)
            {
                value = value.ToLowerInvariant();
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

        // nested classes
        private class EndPointEqualityComparerImpl : IEqualityComparer<EndPoint>
        {
            public bool Equals(EndPoint x, EndPoint y)
            {
                if (x == null && y == null)
                {
                    return true;
                }
                else if (x == null || y == null)
                {
                    return false;
                }

                // mono has a bug in DnsEndPoint.Equals, so if the types aren't
                // equal, it will throw a null reference exception.
                if (x.GetType() != y.GetType())
                {
                    return false;
                }

                return x.Equals(y);
            }

            public int GetHashCode(EndPoint obj)
            {
                return obj.GetHashCode();
            }
        }

    }
}
