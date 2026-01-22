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

namespace MongoDB.Driver.Core;

/// <summary>
/// Represents an error that occurred while establishing a connection to the proxy.
/// </summary>
public class MongoProxyConnectionException : MongoException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MongoProxyConnectionException"/> class.
    /// </summary>
    /// <param name="exception">The inner exception.</param>
    public MongoProxyConnectionException(string message, Exception exception)
        : base(message, exception)
    {
    }

    internal static MongoProxyConnectionException FromException(Exception ex) =>
        new MongoProxyConnectionException("An error occurred while establishing connection to the configured proxy server. See InnerException for more details.", ex);
}

