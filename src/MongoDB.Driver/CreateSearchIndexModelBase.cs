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

namespace MongoDB.Driver;

/// <summary>
/// Abstract base class for search and vector index definitions. Concrete implementations are
/// <see cref="CreateSearchIndexModel"/> and <see cref="CreateVectorIndexModel{TDocument}"/>
/// </summary>
public abstract class CreateSearchIndexModelBase
{
    /// <summary>Gets the index name.</summary>
    /// <value>The index name.</value>
    public virtual string Name { get; }

    /// <summary>Gets the index type.</summary>
    /// <value>The index type.</value>
    public abstract SearchIndexType? Type { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSearchIndexModelBase"/> class.
    /// </summary>
    /// <param name="name">The index name.</param>
    protected CreateSearchIndexModelBase(string name)
    {
        Name = name;
    }
}
