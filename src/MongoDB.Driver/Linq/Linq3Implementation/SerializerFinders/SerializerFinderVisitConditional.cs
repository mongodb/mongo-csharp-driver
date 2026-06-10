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

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal partial class SerializerFinderVisitor
{
    protected override Expression VisitConditional(ConditionalExpression node)
    {
        var ifTrueExpression = node.IfTrue;
        var ifFalseExpression = node.IfFalse;

        DeduceConditionalSerializers();
        base.VisitConditional(node);
        DeduceConditionalSerializers();

        return node;

        void DeduceConditionalSerializers()
        {
            DeduceBaseTypeAndDerivedTypeSerializers(node, ifTrueExpression);
            DeduceBaseTypeAndDerivedTypeSerializers(node, ifFalseExpression);
            DeduceBaseTypeAndDerivedTypeSerializers(node, ifTrueExpression); // call a second time in case ifFalse is the only known serializer
        }
    }
}
