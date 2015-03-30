+++
date = "2015-03-18T16:56:14Z"
draft = false
title = "LINQ Tutorial"
[menu.main]
  weight = 40
  pre = "<i class='fa'></i>"
+++

LINQ Tutorial
========================

Introduction
------------

This tutorial covers the support for LINQ queries as of the 1.8 release
of the C\# driver.

You should already have read at least the [Driver Tutorial]({{< relref "driver.md" >}}).

Quickstart
----------

First, add the following additional using statement to your program:

``` {.sourceCode .csharp}
using MongoDB.Driver.Linq;
```

Then, get a reference to a collection variable in the usual way:

``` {.sourceCode .csharp}
var collection = database.GetCollection<TDocument>("collectionname");
```

The basic idea behind writing a LINQ query is to start from a collection
variable and begin the LINQ query by calling the
`AsQueryable<TDocument>()` method. After that it's all standard LINQ.

For example:

``` {.sourceCode .csharp}
var query =
    from e in collection.AsQueryable<Employee>()
    where e.FirstName == "John"
    select e;

foreach (var employee in query)
{
    // process employees named "John"
}
```

You can also write queries using lambda syntax. The previous query would
be written using lambda syntax like this:

``` {.sourceCode .csharp}
var query =
    collection.AsQueryable<Employee>()
    .Where(e => e.FirstName == "John");
```

The C\# compiler translates all queries written using query syntax into
lambda syntax internally anyway, so there is no performance advantage or
penalty to choosing either style. You can also mix and match the styles,
which can be useful when using query operators that are not supported by
the query syntax.

All the code samples in this tutorial show both the query syntax and the
lambda syntax for each query operator and supported where clauses.

Only LINQ queries that can be translated to an equivalent MongoDB query
are supported. If you write a LINQ query that can't be translated you
will get a runtime exception and the error message will indicate which
part of the query wasn't supported.

Supported LINQ query operators
------------------------------

This section documents the supported LINQ query operators.

-   `Any`

    Without a predicate `Any` just tests whether the collection has any
    documents.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        select c)
        .Any();
    // or
    var result =
        collection.AsQueryable<C>()
        .Any();
    ```

-   `Any (with predicate)`

    With a predicate `Any` tests whether the collection has any matching
    documents.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        select c)
        .Any(c => c.X == 1);
    // or
    var result =
        collection.AsQueryable<C>()
        .Any(c => c.X == 1);
    ```

    `Any` with a predicate is not supported after a projection (at least
    not yet). So the following is not valid:

    ``` {.sourceCode .csharp}
    var result =
        collection.AsQueryable<C>()
        .Select(c => c.X)
        .Any(x => x == 1);
    ```

    You can usually rewrite such a query by putting an equivalent
    *where* clause before the projection (in which case you can drop the
    projection).

-   `Count`

    Without a predicate `Count` just returns the number of documents in
    the collection.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        select c)
        .Count();
    // or
    var result =
        collection.AsQueryable<C>()
        .Count();
    ```

-   `Count (with predicate)`

    With a predicate `Count` returns the number of documents that match
    the predicate.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        select c)
        .Count(c => c.X == 1);
    // or
    var result =
        collection.AsQueryable<C>()
        .Count(c => c.X == 1);
    ```

    Note that the predicate can be provided either by a *where* clause
    or as an argument to `Count`, so the following are equivalent to the
    previous query.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        where c.X == 1
        select c)
        .Count();
    // or
    var result =
        collection.AsQueryable<C>()
        .Where(c => c.X == 1)
        .Count();
    ```

    `Count` with a predicate is not supported after a projection (at
    least not yet). So the following is not valid:

    ``` {.sourceCode .csharp}
    var result =
        collection.AsQueryable<C>()
        .Select(c => c.X)
        .Count(x => x == 1);
    ```

    You can usually rewrite such a query by putting an equivalent
    *where* clause before the projection (in which case you can drop the
    projection).

-   `Distinct`

    `Distinct` returns the unique values of a field or property of the
    documents in the collection. You use a projection to identify the
    field or property whose distinct values you want.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        select c.X)
        .Distinct();
    // or
    var result =
        collection.AsQueryable<C>()
        .Select(c => c.X)
        .Distinct();
    ```

    The projection must select a particular field or property of the
    document. If the value of that field or property is represented in
    MongoDB as an array you can also use array indexing to select an
    item from the array.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        select c.A[i])
        .Distinct();
    // or
    var result =
        collection.AsQueryable<C>()
        .Select(c => c.A[i])
        .Distinct();
    ```

-   `ElementAt`

    `ElementAt` returns a particular document from a result set. Often
    you will combine this with a sort order.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        where c.X > 0
        orderby c.X
        select c)
        .ElementAt(index);
    // or
    var result =
        collection.AsQueryable<C>()
        .Where(c => c.X > 0)
        .OrderBy(c => c.X)
        .ElementAt(index);
    ```

    If the result set has fewer documents than index `ElementAt` throws
    an exception.

-   `ElementAtOrDefault`

    `ElementAtOrDefault` is just like `ElementAt` except that if there
    are fewer documents than index it returns null instead of throwing
    an exception.

-   `First`

    `First` returns the first document from a result set. Often you will
    combine this with a sort order.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        where c.X > 0
        orderby c.X
        select c)
        .First();
    // or
    var result =
        collection.AsQueryable<C>()
        .Where(c => c.X > 0)
        .OrderBy(c => c.X)
        .First();
    ```

    If the result set has no documents `First` throws an exception.

-   `First (with predicate)`

    This overload of `First` allows you to provide a predicate as an
    argument to `First`. This is an alternative to using a *where*
    clause.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        orderby c.X
        select c)
        .First(c => c.X > 0);
    // or
    var result =
        collection.AsQueryable<C>()
        .OrderBy(c => c.X)
        .First(c => c.X > 0);
    ```

    `First` with a predicate is not supported after a projection (at
    least not yet). So the following is not valid:

    ``` {.sourceCode .csharp}
    var result =
        collection.AsQueryable<C>()
        .OrderBy(c => c.X)
        .Select(c => c.X)
        .First(x => x > 0);
    ```

    You can usually rewrite such a query by putting an equivalent
    *where* clause before the projection.

    If the result set has no documents `First` with a predicate throws
    an exception.

-   `FirstOrDefault`

    `FirstOrDefault` is just like `First` except that if there are no
    matching documents it returns `null` instead of throwing an
    exception.

-   `FirstOrDefault (with predicate)`

    `FirstOrDefault` with a predicate is just like `First` with a
    predicate except that if there are no matching documents it returns
    `null` instead of throwing an exception.

-   `Last`

    `Last` returns the last document from a result set. Often you will
    combine this with a sort order.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        where c.X > 0
        orderby c.X
        select c)
        .Last();
    // or
    var result =
        collection.AsQueryable<C>()
        .Where(c => c.X > 0)
        .OrderBy(c => c.X)
        .Last();
    ```

    If the result set has no documents `Last` throws an exception.

-   `Last (with predicate)`

    This overload of `Last` allows you to provide a predicate as an
    argument to `Last`. This is an alternative to using a *where*
    clause.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        orderby c.X
        select c)
        .Last(c => c.X > 0);
    // or
    var result =
        collection.AsQueryable<C>()
        .OrderBy(c => c.X)
        .Last(c => c.X > 0);
    ```

    `Last` with a predicate is not supported after a projection (at
    least not yet). So the following is not valid:

    ``` {.sourceCode .csharp}
    var result =
        collection.AsQueryable<C>()
        .OrderBy(c => c.X)
        .Select(c => c.X)
        .Last(x => x > 0);
    ```

    You can usually rewrite such a query by putting an equivalent
    *where* clause before the projection.

    If the result set has no documents `Last` throws an exception.

-   `LastOrDefault`

    `LastOrDefault` is just like `Last` except that if there are no
    matching documents it returns `null` instead of throwing an
    exception.

-   `LastOrDefault (with predicate)`

    `LastOrDefault` with a predicate is just like `Last` with a
    predicate except that if there are no matching documents it returns
    `null` instead of throwing an exception.

-   `LongCount`

    `LongCount` is just like `Count` except that the return value is a
    64-bit integer instead of a 32-bit integer.

-   `LongCount (with predicate)`

    `LongCount` with a predicate is just like `Count` with a predicate
    except that the return value is a 64-bit integer instead of a 32-bit
    integer.

-   `Max`

    `Max` returns the maximum value of a field or property of the
    documents in the collection. You use a projection to identify the
    field or property whose maximum value you want.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        select c.X)
        .Max();
    // or
    var result =
        collection.AsQueryable<C>()
        .Select(c => c.X)
        .Max();
    ```

    The projection must select a particular field or property of the
    document. If the value of that field or property is represented in
    MongoDB as an array you can also use array indexing to select an
    item from the array.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        select c.A[i])
        .Max();
    // or
    var result =
        collection.AsQueryable<C>()
        .Select(c => c.A[i])
        .Max();
    ```

-   `Max (with selector)`

    This overload of `Max` lets you select the field or property whose
    maximum value you want as an argument to `Max` instead of to
    `Select`.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        select c)
        .Max(c => c.X);
    // or
    var result =
        collection.AsQueryable<C>()
        .Max(c => c.X);
    ```

-   `Min`

    `Min` returns the minimum value of a field or property of the
    documents in the collection. You use a projection to identify the
    field or property whose minimum value you want.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        select c.X)
        .Min();
    // or
    var result =
        collection.AsQueryable<C>()
        .Select(c => c.X)
        .Min();
    ```

    The projection must select a particular field or property of the
    document. If the value of that field or property is represented in
    MongoDB as an array you can also use array indexing to select an
    item from the array.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        select c.A[i])
        .Min();
    // or
    var result =
        collection.AsQueryable<C>()
        .Select(c => c.A[i])
        .Min();
    ```

-   `Min (with selector)`

    This overload of `Min` lets you select the field or property whose
    minimum value you want as an argument to `Min` instead of to
    `Select`.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        select c)
        .Min(c => c.X);
    // or
    var result =
        collection.AsQueryable<C>()
        .Min(c => c.X);
    ```

-   `OfType`

    The OfType operator will insert a discriminator into the query in
    order to be more specific about choosing the correct documents.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>().OfType<D>()
        select c)
    // or
    var result =
        collection.AsQueryable<C>()
        .OfType<D>();
    ```

-   `OrderBy`

    `OrderBy` is used to specify an ascending sort order for the result
    set.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        orderby c.X
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .OrderBy(c => c.X);
    ```

-   `OrderByDescending`

    `OrderByDescending` is used to specify a descending sort order for
    the result set.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        orderby c.X descending
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .OrderByDescending(c => c.X);
    ```

-   `Select`

    `Select` is used to project a new result type from the matching
    documents. A projection must typically be the last operation (with a
    few exceptions like `Distinct`, `Max` and `Min`).

    > **warning**
    >
    > `Select` does not result in fewer fields being returned from the
    > server. The entire document is pulled back and passed to the
    > native `Select` method. Therefore, the projection is performed
    > client side.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        select new { c.X, c.Y };
    // or
    var query =
        collection.AsQueryable<C>()
        .Select(c => new { c.X, c.Y });
    ```

-   `Single`

    `Single` returns the first and only document from a result set.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        where c.X > 0
        orderby c.X
        select c)
        .Single();
    // or
    var result =
        collection.AsQueryable<C>()
        .Where(c => c.X > 0)
        .OrderBy(c => c.X)
        .Single();
    ```

    If the result set has no documents or multiple documents `Single`
    throws an exception.

-   `Single (with predicate)`

    This overload of `Single` allows you to provide a predicate as an
    argument to `Single` . This is an alternative to using a *where*
    clause.

    ``` {.sourceCode .csharp}
    var result =
        (from c in collection.AsQueryable<C>()
        orderby c.X
        select c)
        .Single(c => c.X > 0);
    // or
    var result =
        collection.AsQueryable<C>()
        .OrderBy(c => c.X)
        .Single(c => c.X > 0);
    ```

    `Single` with a predicate is not supported after a projection (at
    least not yet). So the following is not valid:

    ``` {.sourceCode .csharp}
    var result =
        collection.AsQueryable<C>()
        .OrderBy(c => c.X)
        .Select(c => c.X)
        .Single(x => x > 0);
    ```

    You can usually rewrite such a query by putting an equivalent
    *where* clause before the projection.

    If the result set has no documents or multiple documents `Single`
    throws an exception.

-   `SingleOrDefault`

    `SingleOrDefault` is just like `Single` except that if there are no
    matching documents it returns `null` instead of throwing an
    exception.

-   `SingleOrDefault (with predicate)`

    `SingleOrDefault` with a predicate is just like `Single` with a
    predicate except that if there are no matching documents it returns
    `null` instead of throwing an exception.

-   `Skip`

    Use `Skip` to specify how many documents to skip from the beginning
    of the result set. Often you will combine `Skip` with a sort order.

    ``` {.sourceCode .csharp}
    var query =
        (from c in collection.AsQueryable<C>()
        orderby c.X
        select c)
        .Skip(100);
    // or
    var query =
        collection.AsQueryable<C>()
        .OrderBy(c => c.X)
        .Skip(100);
    ```

-   `Take`

    Use `Take` to specify how many documents to return from the server.
    When combining `Take` with `Skip` often you will also specify a sort
    order.

    ``` {.sourceCode .csharp}
    var query =
        (from c in collection.AsQueryable<C>()
        orderby c.X
        select c)
        .Skip(100)
        .Take(100);
    // or
    var query =
        collection.AsQueryable<C>()
        .OrderBy(c => c.X)
        .Skip(100)
        .Take(100);
    ```

-   `ThenBy`

    `ThenBy` is used to specify an additional ascending sort order for
    the result set.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        orderby c.X, c.Y
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .OrderBy(c => c.X)
        .ThenBy(c => c.Y);
    ```

-   `ThenByDescending`

    `ThenByDescending` is used to specify an additional descending sort
    order for the result set.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        orderby c.X, c.Y descending
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .OrderBy(c => c.X)
        .ThenByDescending(c => c.Y);
    ```

-   `Where`

    A *where* clause is used to specify which documents the query should
    return. A *where* clause is a C\# expression that maps the query
    document type to a boolean value. If the expression returns true the
    document "matches" the query and is included in the result set.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.X > 0
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.X > 0);
    ```

    Sometimes a predicate can be supplied in other places besides a
    *where* clause, and it is also possible to have multiple *where*
    clauses. When multiple predicates are involved they are combined
    into a single composite predicate by combining the individual
    predicates with the `&&` operator.

    For example, the following queries are equivalent:

    ``` {.sourceCode .csharp}
    var query =
        (from c in collection.AsQueryable<C>()
        where c.X > 0
        where c.Y > 0)
        .First(c.Z > 0);
    // or
    var query =
        (from c in collection.AsQueryable<C>()
        where c.X > 0 && c.Y > 0 && c.Z > 0)
        .First();
    ```

Supported where clauses
-----------------------

This section documents the supported `where` clauses.

As mentioned earlier, not all C\# expressions are supported as a *where*
clause. You can use this documentation as a guide to what is supported,
or you can just try an expression and see if it works (a runtime
exception is thrown if the *where* clause is not supported).

*Where* clauses are typically introduced using the `Where` query
operator, but the same expressions are supported wherever a predicate is
called for. In some cases multiple *where* clauses and predicates will
be combined, in which case they are combined with the `&&` operator.

> **note**
>
> The 1.4 version of the C\# driver requires that all *where* clauses
> that compare a field or property against a value have the constant on
> the right hand side. This restriction will be lifted in the next
> release.

-   `&&` (**And** operator)

    Sub-expressions can be combined with the `&&` operator to test
    whether all of them are true.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.X > 0 && c.Y > 0
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.X > 0 && c.Y > 0);
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { X : { $gt : 0 }, Y : { $gt : 0 } }
    ```

    In some cases the `And` query can't be flattened as shown, and the
    `$and` operator will be used. The following example matches
    documents where `X` is both a multiple of `2` and a multiple of `3`:

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where (c.X % 2 == 0) && (c.X % 3 == 0)
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => (c.X % 2 == 0) && (c.X % 3 == 0));
    ```

    This is translated to the following MongoDB query using `$and`:

    ``` {.sourceCode .javascript}
    { $and : [{ X : { $mod : [2, 0] } }, { X : { $mod : [3, 0] } }] }
    ```

-   `Any`

    This method is used to test whether an array field or property
    contains any items.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.A.Any()
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.A.Any());
    ```

    matches any document where `A` has `1` or `more` items.

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { A : { $ne : null, $not : { $size : 0 } } }
    ```

-   `Any` With Predicate

    This method is used to test entries in an array. It will generate an
    \$elemMatch condition.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.A.Any(a => a.B == 1)
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.A.Any(a => a.B == 1));
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { A : { $elemMatch : { B : 1 } } }
    ```

    > **note**
    >
    > This will only function when the elements of the enumerable are
    > serialized as a document. There is a server bug preventing this
    > from working against primitives.

-   **Boolean constant**

    This form is mostly for completeness. You will probably use it
    rarely. It allows a boolean constant to be used to either match or
    not match the document.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where true
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => true);
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { _id : { $exists : true } }
    ```

    Which matches all documents since the `_id` is a mandatory field.

-   **Boolean field or property**

    A boolean field or property of the document doesn't have to be
    compared to `true`, it can just be mentioned in the *where* clause
    and there is an implied comparison to `true`.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.B
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.B);
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { B : true }
    ```

-   `Contains` (Enumerable method)

    There are 2 uses for this method depending on context.

    1.  To test whether an array (or array-like) field or property
        contains a particular value:

        ``` {.sourceCode .csharp}
        var query =
            from c in collection.AsQueryable<C>()
            where c.A.Contains(123)
            select c;
        // or
        var query =
            collection.AsQueryable<C>()
            .Where(c => c.A.Contains(123));
        ```

        This is translated to the following MongoDB query:

        ``` {.sourceCode .javascript}
        { A : 123 }
        ```

        This translation relies on the way array fields are treated by
        the MongoDB query language.

    2.  To test whether a field or property is contained in an array (or
        array-like) field.

        ``` {.sourceCode .csharp}
        var local = new [] { 1, 2, 3 };

        var query =
            from c in collection.AsQueryable<C>()
            where local.Contains(c.A)
            select c;
        // or
        var query =
            collection.AsQueryable<C>()
            .Where(c => local.Contains(c.A));
        ```

        This is translated to the following MongoDB query:

        ``` {.sourceCode .javascript}
        { A : { $in : [1, 2, 3] } }
        ```

-   `Contains` (string method)

    This method is used to test whether a string field or property of
    the document contains a particular substring.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.S.Contains("abc")
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.S.Contains("abc"));
    ```

    This is translated to the following MongoDB query (using regular
    expressions):

    ``` {.sourceCode .javascript}
    { S : /abc/ }
    ```

-   `ContainsAll` (LINQ to MongoDB extension method)

    This method is used to test whether an array (or array-like) field
    or property contains all of the provided values.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.A.ContainsAll(new[] { 1, 2, 3 })
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.A.ContainsAll(new[] { 1, 2, 3 }));
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { A : { $all : [1, 2, 3] } }
    ```

-   `ContainsAny` (LINQ to MongoDB extension method)

    This method is used to test whether an array (or array-like) field
    or property contains any of the provided values.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.A.ContainsAny(new[] { 1, 2, 3 })
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.A.ContainsAny(new[] { 1, 2, 3 }));
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { A : { $in : [1, 2, 3] } }
    ```

-   `Count` *method* (array length)

    This method is used to test whether an enumerable field or property
    has a certain count of items.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.L.Count() == 3
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.L.Count() == 3);
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { L : { $size: 3 } }
    ```

-   `Count` *property* (array length)

    This property is used to test whether a list (or list-like) field or
    property has a certain count of items.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.L.Count == 3
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.L.Count == 3);
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { L : { $size: 3 } }
    ```

-   `EndsWith` (string method)

    This method is used to test whether a string field or property of
    the document ends with a particular substring.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.S.EndsWith("abc")
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.S.EndsWith("abc"));
    ```

    This is translated to the following MongoDB query (using regular
    expressions):

    ``` {.sourceCode .javascript}
    { S : /abc$/ }
    ```

-   `enum` comparisons (`==`, `!=`, `<`, `<=`, `>`, `>=`)

    `enum` fields or properties can be compared to constants of the same
    `enum` type. The relative comparison are based on the value of the
    underlying integer type.

    ``` {.sourceCode .csharp}
    public enum E { None, A, B };

    var query =
        from c in collection.AsQueryable<C>()
        where c.E == E.A
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.E == E.A);
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { E : 1 }
    ```

    The LINQ implementation takes the representation of serialized
    values into account, so if you have configured your class map to
    store enums as string values instead of integer values the MongoDB
    query would instead be:

    ``` {.sourceCode .javascript}
    { E : "A" }
    ```

-   `GetType` (Type method)

    This is exactly like the OfType method. It will generate a
    discriminator "and"ed with the other predicates.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.GetType() == typeof(D)
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.GetType() == typeof(D));
    ```

    This is translated roughly to the following MongoDB query depending
    on how your discriminators are created.

    ``` {.sourceCode .javascript}
    { _t : "D" }
    ```

-   `In` (LINQ to MongoDB extension method)

    The `In` method is used to test whether a field or property is equal
    any of a set of provided values.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.X.In(new [] { 1, 2, 3 })
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c.X.In(new [] { 1, 2, 3 }));
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { X : { $in : [1, 2, 3] } }
    ```

-   `Inject`

    `Inject` is a pseudo-method that is used to inject a lower level
    MongoDB query into a LINQ query. The following query looks for `X`
    values that are larger than 0 and are 64-bit integers.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.X > 0 && Query.Type("X", BsonType.Int64).Inject()
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.X > 0 && Query.Type("X", BsonType.Int64).Inject());
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { X : { $gt : 0, $type : 18 } }
    ```

-   `is` C\# keyword

    This is exactly like the OfType method. It will generate a
    discriminator "and"ed with the other predicates.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c is D && ((D)c).B == 1
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c is D && ((D)c).B == 1);
    ```

    This is translated to the something similar to the following,
    depending on how your discriminators are setup.

    ``` {.sourceCode .javascript}
    { _t : "D", B : 1 }
    ```

-   `IsMatch` (regular expression method)

    This method is used to test whether a string field or property
    matches a regular expression.

    ``` {.sourceCode .csharp}
    var regex = new Regex("^abc");
    var query =
        from c in collection.AsQueryable<C>()
        where regex.IsMatch(c.S)
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => regex.IsMatch(c.S));
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { S : /^abc/ }
    ```

    You can also use the static `IsMatch` method.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where Regex.IsMatch(c.S, "^abc")
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => Regex.IsMatch(c.S, "^abc"));
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { S : /^abc/ }
    ```

-   `Length` (array length)

    This method is used to test whether an array (or array-like) field
    or property has a certain count of items.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.A.Length == 3
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.A.Length == 3);
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { A : { $size: 3 } }
    ```

-   `%` (**Mod** operator)

    This operator is used to test the result of the mod operator against
    a field or property of the document. The following query matches all
    the documents where `X` is odd.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.X % 2 == 1
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.X % 2 == 1);
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { X : { $mod : [2, 1] } }
    ```

-   `!` (**Not** operator)

    The `!` operator is used to reverse the sense of a test.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where !(c.X > 1)
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => !(c.X > 1));
    ```

    This is translated into the following MongoDB query:

    ``` {.sourceCode .javascript}
    { X : { $not : { $gt : 1 } } }
    ```

    > **note**
    >
    > `!(c.X > 1)` is not equivalent to `(c.X <= 1)` in cases where
    > `c.X` is missing or does not have a numeric type.

-   Numeric comparisons (`==`, `!=`, `<`, `<=`, `>`, `>=`)

    Numeric fields or properties can be compared using any of the above
    operators.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.X == 0 && c.Y < 100
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.X == 0 && c.Y < 100);
    ```

    This is translated into the following MongoDB query:

    ``` {.sourceCode .javascript}
    { X : 0, Y : { $lt : 100 } }
    ```

-   `||` (**Or** operator)

    Sub-expressions can be combined with the `||` operator to test
    whether any of them is true.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.X > 0 || c.Y > 0
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.X > 0 || c.Y > 0);
    ```

    This is translated to the following MongoDB query:

    ``` {.sourceCode .javascript}
    { $or : [{ X : { $gt : 0 } }, { Y : { $gt : 0 } }] }
    ```

-   `StartsWith` (string method)

    This method is used to test whether a string field or property of
    the document starts with a particular substring.

    ``` {.sourceCode .csharp}
    var query =
        from c in collection.AsQueryable<C>()
        where c.S.StartsWith("abc")
        select c;
    // or
    var query =
        collection.AsQueryable<C>()
        .Where(c => c.S.StartsWith("abc"));
    ```

    This is translated to the following MongoDB query (using regular
    expressions):

    ``` {.sourceCode .javascript}
    { S : /^abc/ }
    ```

-   `ToLower`, `ToLowerInvariant`, `ToUpper`, `ToUpperInvariant` (string
    method)

    These methods are used to test whether a string field or property of
    the document matches a value in a case-insensitive manner.

``` {.sourceCode .csharp}
var query =
    from c in collection.AsQueryable<C>()
    where c.S.ToLower() == "abc"
    select c;
// or
var query =
    collection.AsQueryable<C>()
    .Where(c => c.S.ToLower() == "abc");
```

> This is translated to the following MongoDB query (using regular
> expressions):
>
> ``` {.sourceCode .javascript}
> { S : /^abc$/i }
> ```
