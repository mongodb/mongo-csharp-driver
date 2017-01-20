=======================
Connection String Tests
=======================

The YAML and JSON files in this directory tree are platform-independent tests
that drivers can use to prove their conformance to the Read and Write Concern 
specification.

Converting to JSON
------------------

The tests are written in YAML because it is easier for humans to write and read,
and because YAML includes a standard comment format. A JSONified version of each
YAML file is included in this repository. Whenever you change the YAML,
re-convert to JSON. One method to convert to JSON is with
`jsonwidget-python <http://jsonwidget.org/wiki/Jsonwidget-python>`_::

    pip install PyYAML urwid jsonwidget
    make

Or instead of "make"::

    for i in `find . -iname '*.yml'`; do
        echo "${i%.*}"
        jwc yaml2json $i > ${i%.*}.json
    done

Alternatively, you can use `yamljs <https://www.npmjs.com/package/yamljs>`_::

    npm install -g yamljs
    yaml2json -s -p -r .

Version
-------

Files in the "specifications" repository have no version scheme. They are not
tied to a MongoDB server version, and it is our intention that each
specification moves from "draft" to "final" with no further versions; it is
superseded by a future spec, not revised.

However, implementers must have stable sets of tests to target. As test files
evolve they will be occasionally tagged like "uri-tests-tests-2015-07-16", until
the spec is final.

Format
------

Each YAML file contains an object with a single ``tests`` key. This key is an
array of test case objects, each of which have the following keys:

- ``description``: A string describing the test.
- ``uri``: A string containing the URI to be parsed.
- ``writeConcern:`` A document indicating the expected write concern.
- ``isAcknowledged:`` A boolean indicating whether the write concern is 
  acknowledged.
- ``readConcern:`` A document indicating the expected read concern.
- ``error:``: a boolean indicating if parsing the uri should result in an error, or the string
  'optional' indicating that reporting of an error is optional

If a test case includes a null value for one of these keys,
no assertion is necessary. This both simplifies parsing of the
test files (keys should always exist) and allows flexibility for drivers that
might substitute default values *during* parsing.

Use as unit tests
=================

Testing whether a URI is valid or not should simply be a matter of checking
whether URI parsing raises an error or exception.
Testing for emitted warnings may require more legwork (e.g. configuring a log
handler and watching for output).
