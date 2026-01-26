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
using System.Threading;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver;

internal sealed class TokenBucket
{
    private const double Capacity = 1000d;
    private double _tokens = Capacity;

    public double Tokens => _tokens;

    public bool Consume(double tokens)
    {
        Ensure.IsGreaterThan(tokens, 0, nameof(tokens));

        while (true)
        {
            var current = _tokens;
            if (current < tokens)
            {
                return false;
            }

            var updated = current - tokens;
            var original = Interlocked.CompareExchange(ref _tokens, updated, current);
            if (original == current)
            {
                return true;
            }
        }
    }

    public void Deposit(double tokens)
    {
        Ensure.IsGreaterThan(tokens, 0, nameof(tokens));

        while (true)
        {
            var current = _tokens;
            var updated = Math.Min(current + tokens, Capacity);

            var original = Interlocked.CompareExchange(ref _tokens, updated, current);
            if (original == current)
            {
                return;
            }
        }
    }
}