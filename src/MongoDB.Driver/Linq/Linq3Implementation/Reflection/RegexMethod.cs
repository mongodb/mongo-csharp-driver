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

using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class RegexMethod
    {
        // private static fields
        private static readonly MethodInfo __isMatch;
        private static readonly MethodInfo __staticIsMatch;
        private static readonly MethodInfo __staticIsMatchWithOptions;

        // static constructor
        static RegexMethod()
        {
            __isMatch = ReflectionInfo.Method((Regex regex, string input) => regex.IsMatch(input));
            __staticIsMatch = ReflectionInfo.Method((string input, string pattern) => Regex.IsMatch(input, pattern));
            __staticIsMatchWithOptions = ReflectionInfo.Method((string input, string pattern, RegexOptions options) => Regex.IsMatch(input, pattern, options));
        }

        // public properties
        public static MethodInfo IsMatch => __isMatch;
        public static MethodInfo StaticIsMatch => __staticIsMatch;
        public static MethodInfo StaticIsMatchWithOptions => __staticIsMatchWithOptions;

        // public methods
        public static bool IsMatchMethod(MethodCallExpression expression, out Expression inputExpression, out Regex regex)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(__isMatch))
            {
                var objectExpression = expression.Object;
                if (objectExpression is ConstantExpression objectConstantExpression)
                {
                    regex = (Regex)objectConstantExpression.Value;
                    inputExpression = arguments[0];
                    return true;
                }
            }

            if (method.IsOneOf(__staticIsMatch, __staticIsMatchWithOptions))
            {
                inputExpression = arguments[0];
                var patternExpression = arguments[1];
                var optionsExpression = arguments.Count < 3 ? null : arguments[2];

                string pattern;
                if (patternExpression is ConstantExpression patternConstantExpression)
                {
                    pattern = (string)patternConstantExpression.Value;
                }
                else
                {
                    goto returnFalse;
                }

                var options = RegexOptions.None;
                if (optionsExpression != null)
                {
                    if (optionsExpression is ConstantExpression optionsConstantExpression)
                    {
                        options = (RegexOptions)optionsConstantExpression.Value;
                    }
                    else
                    {
                        goto returnFalse;
                    }
                }

                regex = new Regex(pattern, options);
                return true;
            }

        returnFalse:
            inputExpression = null;
            regex = null;
            return false;
        }
    }
}
