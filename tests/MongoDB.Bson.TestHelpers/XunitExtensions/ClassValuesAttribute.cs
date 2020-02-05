/* Copyright 2020-present MongoDB Inc.
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

namespace MongoDB.Bson.TestHelpers.XunitExtensions
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ClassValuesAttribute : Attribute, IValueGeneratorAttribute
    {
        private readonly Type _classType;

        public ClassValuesAttribute(Type classType)
        {
            _classType = classType;
        }

        public object[] GenerateValues()
        {
            var generator = (IValueGenerator)Activator.CreateInstance(_classType) as IValueGenerator;
            if (generator == null)
            {
                throw new ArgumentException($"The type {_classType} must implement the {nameof(IValueGenerator)} interface.");
            }
            return generator.GenerateValues();
        }
    }
}
