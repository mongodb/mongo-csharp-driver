+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Handling Schema Changes"
[menu.main]
  parent = "Mapping Classes"
  identifier = "Handling Schema Changes"
  weight = 30
  pre = "<i class='fa'></i>"
+++

## Handling Schema Changes

Just because MongoDB is schema-less does not mean that your code can handle a schema-less document. Most likely, if you are using a statically typed language like C# or VB.NET, then your code is not flexible and needs to be mapped to a known schema.

There are a number of different ways that a schema can change from one version of your application to the next.

1. [A new member is added]({{< relref "#a-member-has-been-added" >}})
1. [A member is removed]({{< relref "#a-member-has-been-removed" >}})
1. [A member is renamed]({{< relref "#a-member-has-been-renamed" >}})
1. [The type of a member is changed]({{< relref "#the-type-of-a-member-is-changed" >}})
1. [The representation of a member is changed]({{< relref "#the-representation-of-a-member-is-changed" >}})

How you handle these is up to you. There are two different strategies:

1. Write an upgrade script.
1. Incrementally update your documents as they are used.

The easiest strategy is to write an upgrade script. There is effectively no difference to this method between a relational database (SQL Server, Oracle) and MongoDB. Identify the documents that need to be changed and update them.

Alternatively, and not supportable in most relational databases, is the incremental upgrade. The idea is that your documents get updated as they are used. Documents that are never used never get updated. Because of this, there are some definite pitfalls you will need to be aware of.

First, queries against a schema where half the documents are version 1 and half the documents are version 2 could go awry. For instance, if you rename an element, then your query will need to test both the old element name and the new element name to get all the results.

Second, any incremental upgrade code must stay in the code-base until all the documents have been upgraded. For instance, if there have been 3 versions of a document, [1, 2, and 3] and we remove the upgrade code from version 1 to version 2, any documents that still exist as version 1 are un-upgradeable.


## A Member Has Been Added

When a new member is added to an entity, there is nothing that needs to be done other than restarting the application if you are using the auto mapping features. If not, then you will manually need to map the member in the same way all the other members are getting mapped.

Existing documents will not have this element and it will show up in your class with its default value. You can, of course, specify a default value.


## A Member Has Been Removed

When a member has been removed from am entity, it will continue to exist in the documents. The serializer will throw an exception when this element is seen because it doesn’t know what to do with it. See the sections on [supporting extra elements]({{< relref "reference\bson\mapping\index.md#supporting-extra-elements" >}}) and [ignoring extra elements]({{< relref "reference\bson\mapping\index.md#ignoring-extra-elements" >}}) for information on how to deal with this.


## A Member Has Been Renamed

When a member has been renamed, it will exist in old documents with the old name and in new documents with the new name. The way to handle incremental upgrades for this rename would be to implement an [ExtraElements]({{< relref "reference\bson\mapping\index.md#supporting-extra-elements" >}}) member in conjunction with [ISupportInitialize]({{< relref "reference\bson\mapping\index.md#issuportinitialize" >}}). 

For example, let’s say that a class used to have a `Name` property which has now been split into a `FirstName` and a `LastName` property.

```csharp
public class MyClass : ISupportInitialize 
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    [BsonExtraElements]
    public IDictionary<string, object> ExtraElements { get; set; }

    void ISupportInitialize.BeginInit() 
    {
        // nothing to do at beginning
    }

    void ISupportInitialize.EndInit() 
    {
        object nameValue;
        if (!ExtraElements.TryGetValue("Name", out nameValue)) {
            return;
        }

        var name = (string)nameValue;

        // remove the Name element so that it doesn't get persisted back to the database
        ExtraElements.Remove("Name");

        // assuming all names are "First Last"
        var nameParts = name.Split(' ');

        FirstName = nameParts[0];
        LastName = nameParts[1];
    }
}
```

## The Type of a Member Is Changed

If the .NET type is compatible with the old type (an integer is changed to a double), then everything will continue to work. Otherwise, a custom serializer or a migration script will be required.


## The Representation of a Member Is Changed

If the [representation of a member]({{< relref "reference\bson\mapping\index.md#representation" >}}) is changed and the representations are compatible, then everything will continue to work. Otherwise, a custom serializer or a migration script will be required.