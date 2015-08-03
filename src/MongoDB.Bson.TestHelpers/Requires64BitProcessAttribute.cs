/* Copyright 2015 MongoDB Inc.
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
using NUnit.Framework;

namespace MongoDB.Bson.TestHelpers
{
    [AttributeUsage(AttributeTargets.Method)]
    public class Requires64BitProcessAttribute : CategoryAttribute, ITestAction
    {
        // constructors
        public Requires64BitProcessAttribute()
            : base("Requires64Bit")
        {
        }

        // public properties
        public ActionTargets Targets
        {
            get { return ActionTargets.Test; }
        }

        // public methods
        public void AfterTest(TestDetails details)
        {
        }

        public void BeforeTest(TestDetails details)
        {
            Ensure64Bit();
        }

        // private methods
        private void Ensure64Bit()
        {
            if (IntPtr.Size < 8)
            {
                Assert.Ignore("This test requires a 64-bit process.");
            }
        }
    }
}
