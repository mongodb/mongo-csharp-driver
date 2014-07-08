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
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Misc
{
    public static class TimeSpanParser
    {
        // methods
        public static string ToString(TimeSpan value)
        {
            throw new NotImplementedException();
        }

        public static bool TryParse(string s, out TimeSpan value)
        {
            if (!string.IsNullOrEmpty(s))
            {
                s = s.ToLowerInvariant();
                var end = s.Length - 1;

                var multiplier = 1000; // default units are seconds
                if (s[end] == 's')
                {
                    if (s[end - 1] == 'm')
                    {
                        s = s.Substring(0, s.Length - 2);
                        multiplier = 1;
                    }
                    else
                    {
                        s = s.Substring(0, s.Length - 1);
                        multiplier = 1000;
                    }
                }
                else if (s[end] == 'm')
                {
                    s = s.Substring(0, s.Length - 1);
                    multiplier = 60 * 1000;
                }
                else if (s[end] == 'h')
                {
                    s = s.Substring(0, s.Length - 1);
                    multiplier = 60 * 60 * 1000;
                }
                else if (s.IndexOf(':') != -1)
                {
                    return TimeSpan.TryParse(s, out value);
                }

                double multiplicand;
                var numberStyles = NumberStyles.None;
                if (double.TryParse(s, numberStyles, CultureInfo.InvariantCulture, out multiplicand))
                {
                    value = TimeSpan.FromMilliseconds(multiplicand * multiplier);
                    return true;
                }
            }

            value = default(TimeSpan);
            return false;
        }
    }
}
