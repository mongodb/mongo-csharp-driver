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

using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Configuration;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Logging
{
    public class LoggerFactoryCategoryDecoratorTests
    {
        [Theory]
        [InlineData("MongoDB.Driver.Core.Logging.LogCategories.Command", "MongoDB.Command")]
        [InlineData("MongoDB.Driver.Core.Logging.LogCategories.NewCategory", "MongoDB.NewCategory")]
        [InlineData("MongoDB.Bson.SomeType", "MongoDB.Internal.SomeType")]
        [InlineData("MongoDB.Bson.Namespace.SomeType", "MongoDB.Internal.SomeType")]
        [InlineData("MongoDB.Driver.SomeType", "MongoDB.Internal.SomeType")]
        [InlineData("MongoDB.Driver.Namespace.SomeType", "MongoDB.Internal.SomeType")]
        [InlineData("MongoDB.Driver.Core.SomeType", "MongoDB.Internal.SomeType")]
        [InlineData("MongoDB.Driver.Core.Namespace.SomeType", "MongoDB.Internal.SomeType")]
        [InlineData("MongoDB.Bson.Tests.SomeType", "MongoDB.Tests.SomeType")]
        [InlineData("MongoDB.Bson.Tests.Namespace.SomeType", "MongoDB.Tests.SomeType")]
        [InlineData("MongoDB.Driver.Tests.Namespace.SomeType", "MongoDB.Tests.SomeType")]
        [InlineData("MongoDB.Driver.Core.Tests.SomeType", "MongoDB.Tests.SomeType")]
        [InlineData("MongoDB.Driver.Core.Tests.NameSpace.SomeType", "MongoDB.Tests.SomeType")]
        [InlineData("MongoDB.Driver.Core.TestHelpers.SomeType", "MongoDB.Tests.SomeType")]
        [InlineData("MongoDB.Driver.Core.TestHelpers.NameSpace.SomeType", "MongoDB.Tests.SomeType")]
        [InlineData("MongoDB", "MongoDB")]
        [InlineData("Random", "Random")]
        internal void DecorateCategories_should_return_correct_category(string providedCategory, string expectedCatergory)
        {
            var underlyingFactory = new Mock<ILoggerFactory>();
            var loggingSettings = new LoggingSettings(underlyingFactory.Object);
            var decoratedFactory = loggingSettings.ToInternalLoggerFactory();

            decoratedFactory.CreateLogger(providedCategory);
            underlyingFactory.Verify(f => f.CreateLogger(expectedCatergory), Times.Once);
        }
    }
}
