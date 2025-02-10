# Testing Binary subtype 9: Vector

The JSON files in this directory tree are platform-independent tests that drivers can use to prove their conformance to
the specification.

These tests focus on the roundtrip of the list of numbers as input/output, along with their data type and byte padding.

Additional tests exist in `bson_corpus/tests/binary.json` but do not sufficiently test the end-to-end process of Vector
to BSON. For this reason, drivers must create a bespoke test runner for the vector subtype.

## Format

The test data corpus consists of a JSON file for each data type (dtype). Each file contains a number of test cases,
under the top-level key "tests". Each test case pertains to a single vector. The keys provide the specification of the
vector. Valid cases also include the Canonical BSON format of a document {test_key: binary}. The "test_key" is common,
and specified at the top level.

#### Top level keys

Each JSON file contains three top-level keys.

- `description`: human-readable description of what is in the file
- `test_key`: name used for key when encoding/decoding a BSON document containing the single BSON Binary for the test
    case. Applies to *every* case.
- `tests`: array of test case objects, each of which have the following keys. Valid cases will also contain additional
    binary and json encoding values.

#### Keys of individual tests cases

- `description`: string describing the test.
- `valid`: boolean indicating if the vector, dtype, and padding should be considered a valid input.
- `vector`: (required if valid is true) list of numbers
- `dtype_hex`: string defining the data type in hex (e.g. "0x10", "0x27")
- `dtype_alias`: (optional) string defining the data dtype, perhaps as Enum.
- `padding`: (optional) integer for byte padding. Defaults to 0.
- `canonical_bson`: (required if valid is true) an (uppercase) big-endian hex representation of a BSON byte string.

## Required tests

#### To prove correct in a valid case (`valid: true`), one MUST

- encode a document from the numeric values, dtype, and padding, along with the "test_key", and assert this matches the
    canonical_bson string.
- decode the canonical_bson into its binary form, and then assert that the numeric values, dtype, and padding all match
    those provided in the JSON.

Note: For floating point number types, exact numerical matches may not be possible. Drivers that natively support the
floating-point type being tested (e.g., when testing float32 vector values in a driver that natively supports float32),
MUST assert that the input float array is the same after encoding and decoding.

#### To prove correct in an invalid case (`valid:false`), one MUST

- if the vector field is present, raise an exception when attempting to encode a document from the numeric values,
    dtype, and padding.
- if the canonical_bson field is present, raise an exception when attempting to deserialize it into the corresponding
    numeric values, as the field contains corrupted data.

## FAQ

- What MongoDB Server version does this apply to?
    - Files in the "specifications" repository have no version scheme. They are not tied to a MongoDB server version.
