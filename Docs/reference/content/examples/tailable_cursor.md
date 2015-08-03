+++
date = "2015-05-26T15:36:56Z"
draft = false
title = "Using a Tailable Cursor"
[menu.main]
  parent = "Examples"
  identifier = "Using a Tailable Cursor"
  weight = 40
  pre = "<i class='fa'></i>"
+++

## Using a Tailable Cursor

MongoDB offers the option to watch a [capped collection]({{< docsref "manual/core/capped-collections/" >}}) for changes using a [tailable cursor]({{< docsref "manual/tutorial/create-tailable-cursor/" >}}).

The code below "tails" the capped collection and outputs documents to the console as they are added. The method also handles the possibility of a dead cursor by tracking the field `insertDate`. New documents are added with increasing values of `insertDate`.

{{% note %}}Even though we are using [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}) below, it is possible to use an application defined class by replacing the [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}) references with that of your application defined class.{{% /note %}}

```csharp
private static async Task TailCollectionAsync(IMongoCollection<BsonDocument> collection)
{
    // Set lastValue to the smallest value possible
    BsonValue lastValue = BsonMinKey.Value;
    
    var options = new FindOptions<BsonDocument> 
    { 
        // Our cursor is a tailable cursor and informs the server to await
        CursorType = CursorType.TailableAwait
    };
    
    // Initially, we don't have a filter. An empty BsonDocument matches everything.
    BsonDocument filter = new BsonDocument();
    
    // NOTE: This loops forever. It would be prudent to provide some form of 
    // an escape condition based on your needs; e.g. the user presses a key.
    while (true)
    {
        // Start the cursor and wait for the initial response
        using (var cursor = await collection.FindAsync(filter, options))
        {
            // This callback will get invoked with each new document found
            await cursor.ForEachAsync(document =>
            {
                // Set the last value we saw 
                lastValue = document["insertDate"];
                
                // Write the document to the console.
                await Console.WriteLineAsync(document.ToString());
            });
        }

        // The tailable cursor died so loop through and restart it
        // Now, we want documents that are strictly greater than the last value we saw
        filter = new BsonDocument("$gt", new BsonDocument("insertDate", lastId));
    }
}
```

{{% note %}}If multiple documents might have the exact same insert date, then using the above logic might cause you to miss some documents in the event that the cursor gets restarted. To solve this,
you could track all the documents you've seen by their identifiers for the same `lastValue` and ignore them in the callback. In addition, you would need to change the `$gt` condition to `$gte`{{% /note %}}.