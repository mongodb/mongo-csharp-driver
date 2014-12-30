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

using System.Threading;
using MongoDB.Driver.Core.Bindings;

namespace MongoDB.Driver.Core.SyncExtensionMethods
{
    public static class IReadBindingExtensionMethods
    {
        // static methods
        public static IChannelSourceHandle GetReadChannelSource(this IReadBinding binding, CancellationToken cancellationToken = default(CancellationToken))
        {
            return binding.GetReadChannelSourceAsync(cancellationToken).GetAwaiter().GetResult();
        }
    }

    public static class IWriteBindingExtensionMethods
    {
        // static methods
        public static IChannelSourceHandle GetWriteChannelSource(this IWriteBinding binding, CancellationToken cancellationToken = default(CancellationToken))
        {
            return binding.GetWriteChannelSourceAsync(cancellationToken).GetAwaiter().GetResult();
        }
    }

    public static class IChannelSourceExtensionMethods
    {
        // static methods
        public static IChannelHandle GetChannel(this IChannelSource channelSource, CancellationToken cancellationToken = default(CancellationToken))
        {
            return channelSource.GetChannelAsync(cancellationToken).GetAwaiter().GetResult();
        }
    }
}
