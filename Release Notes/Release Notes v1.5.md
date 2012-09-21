C# Driver Version 1.5 Release Notes
=====================================

This is a major release featuring Typed Builders, enhanced LINQ support, and serialization of custom collections.  There are significant serialization performance improvements, specifically when using class maps.  In addition, there are a number of other enhancements and bug fixes.

File by file change logs are available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.5-Bson.txt
https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.5-Driver.txt

These release notes describe the changes at a higher level, and omit describing
some of the minor changes.

Breaking changes
----------------

- Any custom IBsonSerializer implementations utilizing the methods GetDocumentId, SetDocumentId, GetMemberSerializationInfo, or GetItemSerializationInfo will need to implement the corresponding interface to restore functionality; IBsonIdProvider, IBsonDocumentSerializer, IBsonArraySerializer
- A call to BsonSerializer.RegisterSerializer will now fail for types that implement IBsonSerializable
- The BsonDefaultSerializer methods IsTypeDiscriminated, LookupActualType, LookupDiscriminatorConvention, RegisterDiscriminator, and RegisterDiscriminatorConvention have been moved to BsonSerializer.
- ObjectId.TryParse will now return false instead of throwing an exception when argument is null
- BsonDocumentWrapper no longer ignores null, but wrather wraps it with a BsonNull.Value.  Any code relying on BsonDocumentWrapper ignoring null will need to be evaluated.
- Custom collection/dictionary classes are now serialized by a collection/dictionary serializer instead of the class map serializer.  This means that the items will be serialized instead of the properties and fields.  Any code relying on the old behaviour will need to use BsonClassMap.RegisterClassMap with their custom collection/dictionary to preserve the old behaviour.
- The static Query class used to build queries has changed significantly.  Users of this class can either modify their code or add a using statement alias to the old version.  The DeprecatedQuery version will get dropped in version 2.0.
        
        using Query = MongoDB.Driver.Builders.DeprecatedQuery;

JIRA issues resolved
--------------------

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/secure/IssueNavigator.jspa?mode=hide&requestId=11900

High-Level Library Changes
==========================

Medium Trust
-----------
Support for medium trust is still not here.  The communication protocol with a mongodb server is over TCP, which is dissallowed in vanilla medium trust environments.  However, a slightly altered, custom medium trust permission system allowing sockets enables the driver to run fully.  This can be done by copying the existing medium trust policy file and:

- adding the SocketPermission: 
    
        <SecurityClass Name="SocketPermission" Description="System.Net.SocketPermission, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"/>
    
- adding an IPermission for the new SocketPermission security class:
    
        <IPermission class="SocketPermission" version="1" Unrestricted="true"/>

Support for Azure partial trust should work without changes.  However, the default trust level for Azure is full, so this will only affect you if you have changed your Azure defaults.

Custom Collection Serialization
-------------------------------
Classes that implement IDictionary or IDictionary<TKey, TValue> are now always serialized by a DictionarySerializer. Classes that implement IEnumerable ot IEnumerable< T > are now always serialized by a CollectionSerializer. We believe that a large majority of the time, classes implementing the collection interfaces intend for their items to be persisted rather than any properties (such as Count).  This should enable the use of custom collection classes without any extra effort on your part.

Query Builder
--------------
We have rewritten the static Query class.  The old Query class followed the odd query syntax of mongodb and was found to be somewhat unintuitive for those coming from traditional C# backgrounds and relational databases.  In addition, as we completed the new typed Query< T > static class (discussed below) to aid the building of queries for classes that are using class maps underneath, we found that the difference in the old one and the new one was too stark.

In the older version, a complex query would be built as follows.  

	var query = Query.Or(
	    Query.Exists("fn", true).NE("Jack"),
	    Query.GTE("age", 20).LTE(40));

There are some implied "ands" for the two fields(name and age) that we wanted to remove so that the generated query was as predictable as possible.  The new query syntax is a little more verbose, but we believe overall easier to understand.

	var query = Query.And(
		Query.And(
			Query.Exists("fn"),
			Query.NE("fn", "Jack"))
		Query.And(
			Query.GTE("age", 20),
			Query.LET("age", 40)));

In many cases, you might find that you don't need to change anything, as the syntax is only different when a conjunction is chained.  However, if you use this syntax a lot, then you can still use the old query builder by including a using statement in your files as follows:
	
	using Query = MongoDB.Driver.Builders.DeprecatedQuery;

In version 2.0, we'll be removing the DeprecatedQuery class, so you'll need to update eventually.

Typed Builders
-------------------
In conjunction with the new query builder, we have also included typed builders that mirror all the existing builders.  So, Query has a corresponding Query< T > class, Update has a corresponding Update< T > class, etc...  The huge benefit to this is that you can remove your "magic" strings from your code!  In addition, anyone using custom serializers with class maps has support built-in for value based comparisons.

For instance, given that we have a Person class defined:

	public class Person
	{
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set;}

		[BsonElement("fn")]
		public string FirstName { get; set;}

		[BsonElement("ln")]
		public string LastName { get; set;}

		[BsonElement("age")]
		public int Age { get; set;}
	}	

Without the typed builder, a typical query might look like this:

	ObjectId idFromUserInput = ...;
	var query = Query.And(
		Query.NE("_id", idFromUserInput),
		Query.EQ("fn", "Jack"));

In the above query for a person, we need special knowledge to construct a valid query. First, that "Id" is "_id" in mongodb.  Second, that "_id" is an ObjectId, even though our class exposes it as a string.  And third, that "FirstName" is "fn" in mongodb.  With the typed builders, you can specify this configuration information in one place, either as attributes or through the fluent configuration api, and never need to think about it again, as is demonstrated below, where the exact same query is generated as above.

	string idFromUserInput = ...;
	var query = Query.And(
		Query<Person>.NE(p => p.Id, idFromUserInput),
		Query<Person>.EQ(p => p.FirstName, "Jack"));

In addition, the typed query builders are type-safe, so you can't put an integer value where you have declared your property as a string.  However, the biggest benefit to our internal refactoring is that you can now express your queries as predicates, making the above query even easier and more readable.

	string idFromUserInput = ...;
	var query = Query<Person>.Where(p => p.Id != idFromUserInput && p.FirstName == "Jack");

LINQ Enhancements
-----------------
We continue to make Linq improvements.  Thanks to all who report missing features and problematic queries.  Linq is difficult to implement because IQueryable provides a lot of flexibility and operators that simply aren't supported in MongoDB.  Where implementation makes sense, we will continue to enhance our linq implementations.  As such, we have implemented a number of new operators:

- Added support for & and | operators when both sides evaluate to a boolean.
- Added support for the Any operator when the target is an enumerable of documents.  This will generate an $elemMatch query.  We do NOT support targets that are enumerables of primitives because the mongodb server does not support those.  As soon as the server supports this, we will add this in as well.
- Using ToUpper or ToLower will generate a case-insentive query to mongodb using a regular expression.
- There are a number of times when certain queries will always evaluate to false.  These queries will generate a special query that will utilize an index when possible, but still always evaluate to false on the server.  Don't be surprised to see this query in your query plans: { "_id" : { $type : -1 } }.  In addition, there are some queries that always evaluate to true.  These will generate an empty query document: { }. 
	
	    // { "_id" : { $type : -1 } }
	
	    var query = Query<Person>.Where(p => p.Name.ToUpper() == "Abc");

- Nullable Enums are now supported.
- ContainsKey on any typed impementing IDictionary or IDictionary<K,V> is now supported and will generate a query corresponding to it's serialization format.
- Contains can now be used on any type implementing IEnumerable or IEnumerable< T > and will be serialized to it's corresponding form.  In the case of a local collection containing a field, this would generate an $in clause.

	    var local = new List<int> { 1, 2, 3};

	    // {"Age" : { $in : [1,2,3] } }
	
	    var query = from p in people.AsQueryable()
				where local.Contains(p.Age)
				select p;

- Type queries either via comparison 

        Query<A>.Where(a => a.GetType() == typeof(T)) 
    
    or in LINQ
  
        collection.AsQueryable().OfType<T>() 
      
    are supported for those of you using inheritance in your class maps.  These will generate queries where the type discriminator is checked for the proper value.


GridFS Changes
--------------
In previous releases, downloading files attempted to ensure that indexes existed in the mongodb servers.  This prevented read-only users from downloading files.  We have removed the call to ensure indexes when downloading files so that read-only users can now download files as would be expected.  The downside to this is that if you delete your indexes in GridFS, then there is a good change they will not come back automatically.  So, don't delete your indexes.

In addition, there was a limitation with GridFS files that were larger than 2GB.  This limitation has been removed.

Installer Changes
-----------------
The installer has been rewritten using WIX as the visual studio based installer project will not be supported in future versions of visual studio.  As part of this rewrite, we have removed the installation of the libraries to the GAC.  We will still be strong-signing the assemblies, so you'll be able to install them to the GAC yourself.