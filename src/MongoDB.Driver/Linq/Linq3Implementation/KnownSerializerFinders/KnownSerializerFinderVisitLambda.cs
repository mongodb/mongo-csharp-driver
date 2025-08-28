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
using System.Linq.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.KnownSerializerFinders;

internal partial class KnownSerializerFinderVisitor
{
    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        base.VisitLambda(node);

        // var parameters = node.Parameters;
        // var body = node.Body;
        // if (parameters.Count == 1 &&
        //     parameters[0] is var parameter &&
        //     body.Type == parameter.Type &&
        //     IsKnown(parameter, out var parameterSerializer) &&
        //     IsNotKnown(body))
        // {
        //     // TODO: remove?
        //     // _knownSerializers.AddSerializer(body, parameterSerializer);
        //     throw new Exception("Should not reach here.");
        // }

        return node;
    }
}
