﻿/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class BsonDocumentMethod
    {
        // private static fields
        private static readonly MethodInfo __addWithNameAndValue;
        private static readonly MethodInfo __getItemWithIndex;
        private static readonly MethodInfo __getItemWithName;

        // static constructor
        static BsonDocumentMethod()
        {
            __addWithNameAndValue = ReflectionInfo.Method((BsonDocument document, string name, BsonValue value) => document.Add(name, value));
            __getItemWithIndex = ReflectionInfo.Method((BsonDocument document, int index) => document[index]);
            __getItemWithName = ReflectionInfo.Method((BsonDocument document, string name) => document[name]);
        }

        // public static properties
        public static MethodInfo AddWithNameAndValue => __addWithNameAndValue;
        public static MethodInfo GetItemWithIndex => __getItemWithIndex;
        public static MethodInfo GetItemWithName => __getItemWithName;
    }
}
