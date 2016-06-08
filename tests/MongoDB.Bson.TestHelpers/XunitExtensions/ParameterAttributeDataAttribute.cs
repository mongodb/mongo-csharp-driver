/* Copyright 2016 MongoDB Inc.
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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit.Sdk;

namespace MongoDB.Bson.TestHelpers.XunitExtensions
{
    public class ParameterAttributeDataAttribute : DataAttribute
    {
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            var parameterValueSets = GenerateParameterValueSets(testMethod);
            return CartesianProduct(parameterValueSets);
        }

        private static IEnumerable<object[]> CartesianProduct(object[][] sets)
        {
            if (sets.Length == 1)
            {
                foreach (var value in sets[0])
                {
                    yield return new[] { value };
                }
            }
            else
            {
                var otherValuesCartesianProduct = CartesianProduct(sets.Skip(1).ToArray());
                foreach (var value in sets[0])
                {
                    foreach (var otherValues in otherValuesCartesianProduct)
                    {
                        yield return new[] { value }.Concat(otherValues).ToArray();
                    }
                }
            }
        }

        private object[] GenerateParameterValueSet(ParameterInfo parameterInfo)
        {
            var valueSet = new List<object>();
            foreach (var attribute in parameterInfo.GetCustomAttributes())
            {
                var valueGenerator = attribute as IValueGeneratorAttribute;
                if (valueGenerator != null)
                {
                    valueSet.AddRange(valueGenerator.GenerateValues());
                }
            }
            return valueSet.ToArray();
        }

        private object[][] GenerateParameterValueSets(MethodInfo methodInfo)
        {
            var parameterInfos = methodInfo.GetParameters();
            var parameterValueSets = new object[parameterInfos.Length][];
            for (var i = 0; i < parameterInfos.Length; i++)
            {
                parameterValueSets[i] = GenerateParameterValueSet(parameterInfos[i]);
            }
            return parameterValueSets;
        }
    }
}
