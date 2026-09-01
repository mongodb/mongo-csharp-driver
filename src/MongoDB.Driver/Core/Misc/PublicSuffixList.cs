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
using System.Globalization;
using System.IO;
using System.Resources;
using System.Text;
using System.Threading;

namespace MongoDB.Driver.Core.Misc;

internal static class PublicSuffixList
{
    private const string ResourceName = "MongoDB.Driver.Core.Misc.public_suffix_list.dat";

    private static readonly Lazy<HashSet<string>> __rules = new(LoadRules, LazyThreadSafetyMode.ExecutionAndPublication);

    // public methods
    public static bool IsPublicSuffix(string domain)
    {
        Ensure.IsNotNullOrEmpty(domain, nameof(domain));

        var labelCount = CountLabels(domain);
        return GetPublicSuffixLabelCount(domain, labelCount) == labelCount;
    }

    // private methods
    private static int CountLabels(string domain)
    {
        var count = 1;
        foreach (var c in domain)
        {
            if (c == '.')
            {
                count++;
            }
        }

        return count;
    }

    private static int GetPublicSuffixLabelCount(string domain, int labelCount)
    {
        var rules = __rules.Value;

        // an exception rule prevails over every other matching rule, and its leftmost label is
        // removed before the public suffix is taken
        for (var suffixLabelCount = labelCount; suffixLabelCount >= 1; suffixLabelCount--)
        {
            if (rules.Contains("!" + GetSuffix(domain, suffixLabelCount)))
            {
                return suffixLabelCount - 1;
            }
        }

        // otherwise the matching rule with the most labels prevails. A wildcard rule matches a
        // suffix when its "*" stands for the leftmost of that suffix's labels, so it is sought
        // under the labels remaining to the right of that one.
        for (var suffixLabelCount = labelCount; suffixLabelCount >= 1; suffixLabelCount--)
        {
            if (rules.Contains(GetSuffix(domain, suffixLabelCount)) ||
                (suffixLabelCount >= 2 && rules.Contains("*." + GetSuffix(domain, suffixLabelCount - 1))))
            {
                return suffixLabelCount;
            }
        }

        // when no rule matches, the prevailing rule is "*" and the rightmost label alone is the
        // public suffix
        return 1;
    }

    private static string GetSuffix(string domain, int labelCount)
    {
        var startIndex = domain.Length;
        for (var i = 0; i < labelCount; i++)
        {
            var dotIndex = domain.LastIndexOf('.', startIndex - 1);
            if (dotIndex < 0)
            {
                return domain;
            }

            startIndex = dotIndex;
        }

        return domain.Substring(startIndex + 1);
    }

    private static bool IsAscii(string value)
    {
        foreach (var c in value)
        {
            if (c > 0x7f)
            {
                return false;
            }
        }

        return true;
    }

    private static HashSet<string> LoadRules()
    {
        var assembly = typeof(PublicSuffixList).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream == null)
        {
            throw new MissingManifestResourceException($"The embedded resource \"{ResourceName}\" was not found in {assembly.FullName}.");
        }

        var idnMapping = new IdnMapping();
        var rules = new HashSet<string>(StringComparer.Ordinal);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        // every line of the file is a rule; comment and blank lines have already been stripped
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            rules.Add(ToAsciiRule(line, idnMapping));
        }

        return rules;
    }

    // internationalized rules are stored as Unicode, but the domains they are compared against
    // are Punycode, so the rule is converted to match. The "*." of a wildcard rule and the "!"
    // of an exception rule are markers rather than labels and are not converted.
    private static string ToAsciiRule(string rule, IdnMapping idnMapping)
    {
        if (IsAscii(rule))
        {
            return rule;
        }

        var markerLength = 0;
        if (rule.StartsWith("!", StringComparison.Ordinal))
        {
            markerLength = 1;
        }
        else if (rule.StartsWith("*.", StringComparison.Ordinal))
        {
            markerLength = 2;
        }

        return rule.Substring(0, markerLength) + idnMapping.GetAscii(rule, markerLength);
    }
}
