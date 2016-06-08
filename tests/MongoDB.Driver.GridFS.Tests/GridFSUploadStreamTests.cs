/* Copyright 2015-2016 MongoDB Inc.
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
using System.IO;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests;
using Xunit;

namespace MongoDB.Driver.GridFS.Tests
{
    public class GridFSUploadStreamTests
    {
        // public methods
        [Theory]
        [ParameterAttributeData]
        public void CopyTo_should_throw(
            [Values(false, true)] bool async)
        {
            var bucket = CreateBucket();
            var subject = bucket.OpenUploadStream("Filename");

            using (var destination = new MemoryStream())
            {
                Action action;
                if (async)
                {
                    action = () => subject.CopyToAsync(destination).GetAwaiter().GetResult();
                }
                else
                {
                    action = () => subject.CopyTo(destination);
                }

                action.ShouldThrow<NotSupportedException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Flush_should_not_throw(
            [Values(false, true)] bool async)
        {
            var bucket = CreateBucket();
            var subject = bucket.OpenUploadStream("Filename");

            Action action;
            if (async)
            {
                action = () => subject.FlushAsync(CancellationToken.None).GetAwaiter().GetResult(); ;
            }
            else
            {
                action = () => subject.Flush();
            }

            action.ShouldNotThrow();
        }

        // private methods
        private IGridFSBucket CreateBucket()
        {
            var client = DriverTestConfiguration.Client;
            var databaseNamespace = DriverTestConfiguration.DatabaseNamespace;
            var database = client.GetDatabase(databaseNamespace.DatabaseName);
            return new GridFSBucket(database);
        }
    }
}
