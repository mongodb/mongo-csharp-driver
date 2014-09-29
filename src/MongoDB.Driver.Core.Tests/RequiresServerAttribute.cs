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
using System.Reflection;
using NUnit.Framework;

namespace MongoDB.Driver.Core
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequiresServerAttribute : CategoryAttribute, ITestAction
    {
        // fields
        private readonly string _afterTestMethodName;
        private readonly string _beforeTestMethodName;

        // constructors
        public RequiresServerAttribute(string beforeTestMethodName = null, string afterTestMethodName = null)
            : base("RequiresServer")
        {
            _beforeTestMethodName = beforeTestMethodName;
            _afterTestMethodName = afterTestMethodName;
        }

        // properties
        public ActionTargets Targets
        {
            get { return ActionTargets.Test; }
        }

        // methods
        public void AfterTest(TestDetails details)
        {
            InvokeMethod(details.Fixture, _afterTestMethodName);
        }

        public void BeforeTest(TestDetails details)
        {
            InvokeMethod(details.Fixture, _beforeTestMethodName);
        }

        private void InvokeMethod(object fixture, string methodName)
        {
            if (methodName != null)
            {
                var fixtureType = fixture.GetType();
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                var methodInfo = fixtureType.GetMethod(methodName, bindingFlags);
                if (methodInfo == null)
                {
                    var message = string.Format("Type '{0}' does not contain a method named '{1}'.", fixtureType.Name, methodName);
                    Assert.Fail(message);
                }
                methodInfo.Invoke(methodInfo.IsStatic ? null : fixture, new object[0]);
            }
        }
    }
}
