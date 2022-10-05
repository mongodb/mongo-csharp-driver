============================================
Server Discovery And Monitoring -- Test Plan
============================================

:Spec: 101
:Title: Server Discovery And Monitoring
:Author: A\. Jesse Jiryu Davis
:Advisors: David Golden, Craig Wilson
:Status: Draft
:Type: Standards
:Last Modified: September 8, 2014

See also the YAML test files and their accompanying README in the "tests"
directory.

.. contents::

--------

All servers unavailable
-----------------------

A MongoClient can be constructed without an exception,
even with all seeds unavailable.

Network error writing to primary
--------------------------------

Scenario: With TopologyType ReplicaSetWithPrimary, a write to the primary fails
with a network error other than timeout.

Outcome: The former primary's ServerType MUST become Unknown.
The TopologyType MUST change to ReplicaSetNoPrimary.
The client MUST NOT immediately re-check the former primary.

"Not master" error when reading without SlaveOkay bit
-----------------------------------------------------

Scenario: With TopologyType ReplicaSetWithPrimary, we read from a server we
thought was RSPrimary. Thus the slaveOk bit is not set.

The response's QueryFailure bit is set and the response document is:

    {$err: "not master and slaveOk=false"}

Outcome: The former primary's ServerType MUST become Unknown.
The TopologyType MUST change to ReplicaSetNoPrimary.
The client MUST NOT immediately re-check the former primary.

"Node is recovering" error reading with SlaveOkay bit
-----------------------------------------------------

Scenario: With TopologyType ReplicaSetWithPrimary, we read from a server we
thought was RSSecondary. Thus the slaveOk bit *is* set.

The response's QueryFailure bit is set and the response document is:

    {$err: "not master or secondary; cannot currently read from this replSet member"}

Outcome: The former secondary's ServerType MUST become Unknown.
The TopologyType MUST remain ReplicaSetWithPrimary.
A multi-threaded client MUST immediately re-check the former secondary,
a single-threaded client MUST NOT.

"Node is recovering" error from a write concern error
-----------------------------------------------------

Scenario: With TopologyType ReplicaSetWithPrimary, a write to the primary responds
with the following document:

    { ok: 1, writeConcernError: {code: 91, errmsg: "Replication is being shut down"} }

Outcome: The former primary's ServerType MUST become Unknown.
The TopologyType MUST change to ReplicaSetNoPrimary.
A multi-threaded client MUST immediately re-check the former secondary,
a single-threaded client MUST NOT.

Parsing "not master" and "node is recovering" errors
----------------------------------------------------

For all these example responses,
the client MUST mark the server "Unknown"
and store the error message in the ServerDescription's error field.

Clients MUST NOT depend on any particular field order in these responses.

getLastError
''''''''''''

GLE response after OP_INSERT on an arbiter, secondary, slave,
recovering member, or ghost:

    {ok: 1, err: "not master"}

`Possible GLE response in MongoDB 2.6`_ during failover:

    {ok: 1, err: "replicatedToNum called but not master anymore"}

Note that this error message contains "not master" but does not start with it.

.. _Possible GLE response in MongoDB 2.6: https://jira.mongodb.org/browse/SERVER-9617

Write command
'''''''''''''

Response to an "insert" command on an arbiter, secondary, slave,
recovering member, or ghost:

    {ok: 0, errmsg: "not master"}

Query with slaveOk bit
''''''''''''''''''''''

Response from an arbiter, recovering member, or ghost
when slaveOk is true:

    {$err: "not master or secondary; cannot currently read from this replSet member"}

The QueryFailure bit is set in responseFlags.

Query without slaveOk bit
'''''''''''''''''''''''''

Response from an arbiter, recovering member, ghost, or secondary
when slaveOk is false:

    {$err: "not master and slaveOk=false"}

The QueryFailure bit is set in responseFlags.

Count with slaveOk bit
''''''''''''''''''''''

Command response on an arbiter, recovering member, or ghost
when slaveOk is true:

    {ok: 0, errmsg: "node is recovering"}

Count without slaveOk bit
'''''''''''''''''''''''''

Command response on an arbiter, recovering member, ghost, or secondary
when slaveOk is false:

    {ok: 0, errmsg: "not master"}


Topology discovery and direct connection
----------------------------------------

Topology discovery
''''''''''''''''''

Scenario: given a replica set deployment with a secondary, where HOST
is the address of the secondary, create a MongoClient using
``mongodb://HOST/?directConnection=false`` as the URI.
Attempt a write to a collection.

Outcome: Verify that the write succeeded.

Direct connection
'''''''''''''''''

Scenario: given a replica set deployment with a secondary, where HOST
is the address of the secondary, create a MongoClient using
``mongodb://HOST/?directConnection=true`` as the URI.
Attempt a write to a collection.

Outcome: Verify that the write failed with a NotMaster error.

Existing behavior
'''''''''''''''''

Scenario: given a replica set deployment with a secondary, where HOST
is the address of the secondary, create a MongoClient using
``mongodb://HOST/`` as the URI.
Attempt a write to a collection.

Outcome: Verify that the write succeeded or failed depending on existing
driver behavior with respect to the starting topology.
