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

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc;

/// <summary>
/// This class is called before we process any LINQ expression trees
/// to perform any necessary pre-processing such as CLR compatibility
/// and partial evaluation.
/// </summary>
internal static class LinqExpressionPreprocessor
{
    public static Expression Preprocess(Expression expression)
    {
        expression = ClrCompatExpressionRewriter.Rewrite(expression);
        expression = PartialEvaluator.EvaluatePartially(expression);
        return expression;
    }
}
