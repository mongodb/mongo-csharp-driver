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
using System.Collections.Generic;

namespace MongoDB.Benchmarks.Linq;

public class OrderDocument
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
    public string Status { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string Currency { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public string Notes { get; set; }
    public int ItemCount { get; set; }
    public bool IsPaid { get; set; }
    public string PaymentMethod { get; set; }
    public string ShippingMethod { get; set; }
    public Address ShippingAddress { get; set; }
#pragma warning disable CA2227 // Collection properties should be read only
    public List<OrderItem> Items { get; set; }
#pragma warning restore CA2227
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Zip { get; set; }
    public string Country { get; set; }
}

public class OrderItem
{
    public string ProductId { get; set; }
    public decimal Price { get; set; }
}

public class OrderProjection
{
    public int Id { get; set; }
    public string Customer { get; set; }
    public decimal Total { get; set; }
    public IEnumerable<string> ProductIds { get; set; }
}

public class OrderSummary
{
    public string CustomerName { get; set; }
    public decimal Total { get; set; }
}

public class SetFields
{
    public string Status { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal Total { get; set; }
}

public class GroupResult
{
    public string Status { get; set; }
    public int Count { get; set; }
    public decimal TotalRevenue { get; set; }
}
