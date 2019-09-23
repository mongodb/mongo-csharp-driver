/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Encryption
{
    internal static class KmsProvidersHelper
    {
        public static void EnsureKmsProvidersAreValid(IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders)
        {
            foreach (var kmsProvider in kmsProviders)
            {
                foreach (var option in Ensure.IsNotNull(kmsProvider.Value, nameof(kmsProvider)))
                {
                    var optionValue = Ensure.IsNotNull(option.Value, "kmsProviderOption");
                    var isValid = optionValue is byte[] || optionValue is string;
                    if (!isValid)
                    {
                        throw new ArgumentException($"Invalid kms provider option type: {optionValue.GetType().Name}.");
                    }
                }
            }
        }

        public static bool Equals(IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> x, IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> y)
        {
            return x.IsEquivalentTo(y, KmsProviderIsEquivalentTo);
        }

        // private methods
        private static bool KmsProviderIsEquivalentTo(IReadOnlyDictionary<string, object> x, IReadOnlyDictionary<string, object> y)
        {
            return x.IsEquivalentTo(y, KmsProviderOptionEquals);
        }

        private static bool KmsProviderOptionEquals(object x, object y)
        {
            if (x is byte[] xBytes && y is byte[] yBytes)
            {
                return xBytes.SequenceEqual(yBytes);
            }
            else
            {
                return object.Equals(x, y);
            }
        }
    }
}
