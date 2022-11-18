.. role:: javascript(code)
  :language: javascript

========================================
Connection Monitoring and Pooling (CMAP)
========================================

.. contents::

--------

Introduction
============
Drivers MUST implement all of the following types of CMAP tests:

* Pool unit and integration tests as described in `cmap-format/README.rst <./cmap-format/README.rst>`__
* Pool prose tests as described below in `Prose Tests`_
* Logging tests as described below in `Logging Tests`_

Prose Tests
===========

The following tests have not yet been automated, but MUST still be tested:

#. All ConnectionPoolOptions MUST be specified at the MongoClient level
#. All ConnectionPoolOptions MUST be the same for all pools created by a MongoClient
#. A user MUST be able to specify all ConnectionPoolOptions via a URI string
#. A user MUST be able to subscribe to Connection Monitoring Events in a manner idiomatic to their language and driver
#. When a check out attempt fails because connection set up throws an error,
   assert that a ConnectionCheckOutFailedEvent with reason="connectionError" is emitted.

Logging Tests
=============

Tests for connection pool logging can be found in the `/logging <./logging>`__ subdirectory and are written in the 
`Unified Test Format <../../unified-test-format/unified-test-format.rst>`__. 