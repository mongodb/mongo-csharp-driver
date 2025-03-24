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

using System.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class MongoDBMathMethod
    {
        // private static fields
        private static readonly MethodInfo __degreesToRadians;
        private static readonly MethodInfo __radiansToDegrees;

        // static constructor
        static MongoDBMathMethod()
        {
            __degreesToRadians = ReflectionInfo.Method((double degrees) => MongoDBMath.DegreesToRadians(degrees));
            __radiansToDegrees = ReflectionInfo.Method((double radians) => MongoDBMath.RadiansToDegrees(radians));
        }

        // public properties
        public static MethodInfo DegreesToRadians => __degreesToRadians;
        public static MethodInfo RadiansToDegrees => __radiansToDegrees;
    }
}
