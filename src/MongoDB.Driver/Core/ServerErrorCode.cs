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
    internal enum ServerErrorCode
    {
        // this is not a complete list, more will be added as needed
        // see: https://github.com/mongodb/mongo/blob/master/src/mongo/base/error_codes.yml
        AuthenticationFailed = 18,
        CappedPositionLost = 136,
        CommandNotFound = 59,
        CursorKilled = 237,
        CursorNotFound = 43,
        ElectionInProgress = 216,
        ExceededTimeLimit = 262,
        FailedToSatisfyReadPreference = 133,
        HostNotFound = 7,
        HostUnreachable = 6,
        DuplicateKey = 11000,
        IllegalOperation = 20,
        Interrupted = 11601,
        InterruptedAtShutdown = 11600,
        InterruptedDueToReplStateChange = 11602,
        LegacyNotPrimary = 10058,
        MaxTimeMSExpired = 50,
        NamespaceNotFound = 26,
        NetworkTimeout = 89,
        NotWritablePrimary = 10107,
        NotPrimaryNoSecondaryOk = 13435,
        NotPrimaryOrSecondary = 13436,
        PrimarySteppedDown = 189,
        ReadConcernMajorityNotAvailableYet = 134,
        ReauthenticationRequired = 391,
        RetryChangeStream = 234,
        ShutdownInProgress = 91,
        SocketException = 9001,
        StaleConfig = 13388,
        StaleEpoch = 150,
        StaleShardVersion = 63,
        Unauthorized = 13,
        UnauthorizedServerless = 8000,
        UnknownReplWriteConcern = 79,
        UnsatisfiableWriteConcern = 100,
        WriteConcernFailed = 64
    }
}
