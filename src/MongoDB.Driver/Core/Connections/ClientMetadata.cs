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
using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Connections;

internal sealed class ClientMetadata
{
    private readonly string _applicationName;
    private readonly object _lock = new();
    private volatile LibraryInfo[] _libraryInfos;
    private volatile BsonDocument _clientDocument;

    public ClientMetadata(string applicationName, LibraryInfo libraryInfo)
    {
        _applicationName = applicationName;
        _libraryInfos = libraryInfo != null ? [libraryInfo] : [];
    }

    public BsonDocument GetClientDocument()
    {
        var clientDocument = _clientDocument;
        if (clientDocument != null)
        {
            return clientDocument;
        }

        lock (_lock)
        {
            return _clientDocument ??= ClientDocumentHelper.CreateClientDocument(_applicationName, _libraryInfos);
        }
    }

    public void Append(LibraryInfo libraryInfo)
    {
        Ensure.IsNotNull(libraryInfo, nameof(libraryInfo));

        if (Array.IndexOf(_libraryInfos, libraryInfo) >= 0)
        {
            return;
        }

        lock (_lock)
        {
            var current = _libraryInfos;
            if (Array.IndexOf(current, libraryInfo) >= 0)
            {
                return;
            }

            LibraryInfo[] updated = [..current, libraryInfo];
            _libraryInfos = updated;
            _clientDocument = null;
        }
    }
}
