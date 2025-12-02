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

using System.Linq.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using ExpressionVisitor = System.Linq.Expressions.ExpressionVisitor;

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal partial class SerializerFinderVisitor : ExpressionVisitor
{
    private bool _isMakingProgress = true;
    private readonly SerializerMap _nodeSerializers;
    private int _oldNodeSerializersCount = 0;
    private readonly ExpressionTranslationOptions _translationOptions;
    private bool _useDefaultSerializerForConstants = false; // make as much progress as possible before setting this to true

    public SerializerFinderVisitor(ExpressionTranslationOptions translationOptions, SerializerMap nodeSerializers)
    {
        _nodeSerializers = nodeSerializers;
        _translationOptions = translationOptions;
    }

    public bool IsMakingProgress => _isMakingProgress;

    public void EndPass()
    {
        var newNodeSerializersCount = _nodeSerializers.Count;
        if (newNodeSerializersCount == _oldNodeSerializersCount)
        {
            if (_useDefaultSerializerForConstants)
            {
                _isMakingProgress = false;
            }
            else
            {
                _useDefaultSerializerForConstants = true;
            }
        }
    }

    public void StartPass()
    {
        _oldNodeSerializersCount = _nodeSerializers.Count;
    }

    public override Expression Visit(Expression node)
    {
        if (IsKnown(node, out var nodeSerializer))
        {
            if (nodeSerializer is IIgnoreSubtreeSerializer or IUnknowableSerializer)
            {
                return node; // don't visit subtree
            }
        }

        return base.Visit(node);
    }
}
