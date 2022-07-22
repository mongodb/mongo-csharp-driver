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
using System.Linq;

namespace MongoDB.Driver.Tests
{
    /// <summary>
    /// A static class to hold expected error messages that differ between .NET and Mono.
    /// </summary>
    public static class ExpectedErrorMessage
    {
        // private static fields
        private static string __firstEmptySequence;
        private static string __lastEmptySequence;
        private static string __singleEmptySequence;
        private static string __singleLongSequence;

        // static constructor
        static ExpectedErrorMessage()
        {
            var emptySequence = new List<int>();
            var longElementSequence = new List<int> { 1, 2, 3 };

            try
            {
                emptySequence.First();
            }
            catch (Exception ex)
            {
                __firstEmptySequence = ex.Message;
            }

            try
            {
                emptySequence.Last();
            }
            catch (Exception ex)
            {
                __lastEmptySequence = ex.Message;
            }

            try
            {
                emptySequence.Single();
            }
            catch (Exception ex)
            {
                __singleEmptySequence = ex.Message;
            }

            try
            {
                longElementSequence.Single();
            }
            catch (Exception ex)
            {
                __singleLongSequence = ex.Message;
            }
        }

        // public static properties
        public static string FirstEmptySequence
        {
            get { return __firstEmptySequence; }
        }

        public static string LastEmptySequence
        {
            get { return __lastEmptySequence; }
        }

        public static string SingleEmptySequence
        {
            get { return __singleEmptySequence; }
        }

        public static string SingleLongSequence
        {
            get { return __singleLongSequence; }
        }
    }
}
