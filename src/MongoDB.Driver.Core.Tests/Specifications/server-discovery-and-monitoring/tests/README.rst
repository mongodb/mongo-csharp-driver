=====================================
Server Discovery And Monitoring Tests
=====================================

The YAML and JSON files in this directory tree are platform-independent tests
that drivers can use to prove their conformance to the
Server Discovery And Monitoring Spec.

Converting to JSON
------------------

The tests are written in YAML
because it is easier for humans to write and read,
and because YAML includes a standard comment format.
A JSONified version of each YAML file is included in this repository.
Whenever you change the YAML, re-convert to JSON.
One method to convert to JSON is with
`jsonwidget-python <http://jsonwidget.org/wiki/Jsonwidget-python>`_::

    pip install PyYAML urwid jsonwidget
    make

Or instead of "make":

    for i in `find . -iname '*.yml'`; do
        echo "${i%.*}"
        jwc yaml2json $i > ${i%.*}.json
    done

Version
-------

Files in the "specifications" repository have no version scheme.
They are not tied to a MongoDB server version,
and it is our intention that each specification moves from "draft" to "final"
with no further versions; it is superseded by a future spec, not revised.

However, implementers must have stable sets of tests to target.
As test files evolve they will be occasionally tagged like
"server-discovery-tests-2014-09-10", until the spec is final.

Format
------

Each YAML file has the following keys:

- description: Some text.
- uri: A connection string.
- phases: An array of "phase" objects.

A "phase" of the test sends inputs to the client, then tests the client's
resulting TopologyDescription. Each phase object has two keys:

- responses: An array of "response" objects.
- outcome: An "outcome" object representing the TopologyDescription.

A response is a pair of values:

- The source, for example "a:27017".
  This is the address the client sent the "ismaster" command to.
- An ismaster response, for example `{ok: 1, ismaster: true}`.
  If the response includes an electionId it is shown in extended JSON like
  `{"$oid": "000000000000000000000002"}`.
  The empty response `{}` indicates a network error
  when attempting to call "ismaster".

An "outcome" represents the correct TopologyDescription that results from
processing the responses in the phases so far. It has the following keys:

- topologyType: A string like "ReplicaSetNoPrimary".
- setName: A string with the expected replica set name, or null.
- servers: An object whose keys are addresses like "a:27017", and whose values
  are "server" objects.

A "server" object represents a correct ServerDescription within the client's
current TopologyDescription. It has the following keys:

- type: A ServerType name, like "RSSecondary".
- setName: A string with the expected replica set name, or null.
- electionId: null, or an ObjectId.

Use as unittests
----------------

Drivers should be able to test their server discovery and monitoring logic
without any network I/O, by parsing ismaster responses from the test file
and passing them into the driver code. Parts of the client and monitoring
code may need to be mocked or subclassed to achieve this. `A reference
implementation for PyMongo 3.x is available here
<https://github.com/mongodb/mongo-python-driver/blob/3.0-dev/test/test_discovery_and_monitoring.py>`_.

For each file, create a fresh client object initialized with the file's "uri".

For each phase in the file, parse the "responses" array.
Pass in the responses in order to the driver code.
If a response is the empty object `{}`, simulate a network error.

Once all responses are processed, assert that the phase's "outcome" object
is equivalent to the driver's current TopologyDescription.

Continue until all phases have been executed.
