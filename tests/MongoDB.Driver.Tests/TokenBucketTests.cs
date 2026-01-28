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
using Xunit;

namespace MongoDB.Driver.Tests;

public class TokenBucketTests
{
    [Fact]
    public void Initial_tokens_should_equal_capacity()
    {
        var bucket = new TokenBucket();
        Assert.Equal(1000d, bucket.Tokens);
    }

    [Fact]
    public void Consume_should_succeed_when_enough_tokens()
    {
        var bucket = new TokenBucket();
        var result = bucket.Consume(100);
        Assert.True(result);
        Assert.Equal(900d, bucket.Tokens);
    }

    [Fact]
    public void Consume_should_fail_when_not_enough_tokens()
    {
        var bucket = new TokenBucket();
        var result = bucket.Consume(2000);
        Assert.False(result);
        Assert.Equal(1000d, bucket.Tokens);
    }

    [Fact]
    public void Deposit_should_increase_tokens_up_to_capacity()
    {
        var bucket = new TokenBucket();
        bucket.Consume(500);
        bucket.Deposit(200);
        Assert.Equal(700d, bucket.Tokens);
        bucket.Deposit(500);
        Assert.Equal(1000d, bucket.Tokens);
    }

    [Fact]
    public void Consume_should_throw_when_argument_invalid()
    {
        var bucket = new TokenBucket();
        Assert.Throws<ArgumentOutOfRangeException>(() => bucket.Consume(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => bucket.Consume(-1));
    }

    [Fact]
    public void Deposit_should_throw_when_argument_invalid()
    {
        var bucket = new TokenBucket();
        Assert.Throws<ArgumentOutOfRangeException>(() => bucket.Deposit(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => bucket.Deposit(-1));
    }
}