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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Connections;

/// <summary>
/// Holds the client handshake metadata for a cluster and the library information appended to it after construction.
/// </summary>
internal sealed class ClientMetadata
{
    private readonly string _applicationName;
    private readonly List<LibraryInfo> _libraryInfos = new();
    private readonly object _lock = new();
    private volatile BsonDocument _clientDocument;

    public ClientMetadata(string applicationName, LibraryInfo libraryInfo)
    {
        _applicationName = applicationName;
        if (libraryInfo != null)
        {
            _libraryInfos.Add(Normalize(libraryInfo));
        }
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
            return _clientDocument ??= ClientDocumentHelper.CreateClientDocument(_applicationName, MergeLibraryInfos());
        }
    }

    public void Append(LibraryInfo libraryInfo)
    {
        Ensure.IsNotNull(libraryInfo, nameof(libraryInfo));
        libraryInfo = Normalize(libraryInfo);

        lock (_lock)
        {
            if (_libraryInfos.Contains(libraryInfo))
            {
                return;
            }

            _libraryInfos.Add(libraryInfo);
            _clientDocument = null;
        }
    }

    // empty strings are considered unset, so normalize them to null for deduplication
    private static LibraryInfo Normalize(LibraryInfo libraryInfo)
    {
        var version = string.IsNullOrWhiteSpace(libraryInfo.Version) ? null : libraryInfo.Version;
        var platform = string.IsNullOrWhiteSpace(libraryInfo.Platform) ? null : libraryInfo.Platform;

        if (version == libraryInfo.Version && platform == libraryInfo.Platform)
        {
            return libraryInfo;
        }

        return new LibraryInfo(libraryInfo.Name, version, platform);
    }

    private LibraryInfo MergeLibraryInfos()
    {
        if (_libraryInfos.Count == 0)
        {
            return null;
        }

        // entries are normalized on insertion, so null is the only unset form
        var name = string.Join("|", _libraryInfos.Select(libraryInfo => libraryInfo.Name));
        var version = string.Join("|", _libraryInfos.Where(libraryInfo => libraryInfo.Version != null).Select(libraryInfo => libraryInfo.Version));
        var platform = string.Join("|", _libraryInfos.Where(libraryInfo => libraryInfo.Platform != null).Select(libraryInfo => libraryInfo.Platform));

        return new LibraryInfo(name, version, platform);
    }
}
