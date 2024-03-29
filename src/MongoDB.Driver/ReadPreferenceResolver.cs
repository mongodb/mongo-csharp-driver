/* Copyright 2018-present MongoDB Inc.
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

namespace MongoDB.Driver
{
    internal static class ReadPreferenceResolver
    {
        public static ReadPreference GetEffectiveReadPreference(
            IClientSessionHandle session,
            ReadPreference explicitReadPreference,
            ReadPreference defaultReadPreference)
        {
            if (explicitReadPreference != null)
            {
                return explicitReadPreference;
            }

            if (session.IsInTransaction)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var transactionReadPreference = session.WrappedCoreSession.CurrentTransaction.TransactionOptions.ReadPreference;
#pragma warning restore CS0618 // Type or member is obsolete
                if (transactionReadPreference != null)
                {
                    return transactionReadPreference;
                }
            }

            return defaultReadPreference ?? ReadPreference.Primary;
        }
    }
}
