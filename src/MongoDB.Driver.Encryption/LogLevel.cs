/*
 * Copyright 2019–present MongoDB, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace MongoDB.Driver.Encryption
{
    internal enum LogLevel {
        MONGOCRYPT_LOG_LEVEL_FATAL = 0,
        MONGOCRYPT_LOG_LEVEL_ERROR = 1,
        MONGOCRYPT_LOG_LEVEL_WARNING = 2,
        MONGOCRYPT_LOG_LEVEL_INFO = 3,
        MONGOCRYPT_LOG_LEVEL_TRACE = 4
    };
}
