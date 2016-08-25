# BSON Decimal128 Value Object Tests

These tests follow the (Work In Progress) `"BSON Corpus"` format, more or less.

In pseudo-code, the tests should look like the following:

```

    B  = decode_hex( case["bson"] )
    E  = case["extjson"]
    
    /* Note that "canonical_bson" is not used for the Decimal128 tests
     *  -- but it is used by other upcoming "BSON Corpus" tests
     */

    if "canonical_bson" in case:
        cB = decode_hex( case["canonical_bson"] )
    else:
        cB = B

    if "canonical_extjson" in case:
        cE = decode_extjson( case["canonical_extjson"] )
    else:
        cE = E

    assert encode_bson(decode_bson(B)) == cB                    # B->cB

    if B != cB:
        assert encode_bson(decode_bson(cB)) == cB               # cB->cB

    if "extjson" in case:
        assert encode_extjson(decode_bson(B)) == cE             # B->cE
        assert encode_extjson(decode_extjson(E)) == cE          # E->cE

        if B != cB:
            assert encode_extjson(decode_bson(cB)) == cE        # cB->cE

        if  E != cE:
            assert encode_extjson(decode_extjson(cE)) == cE     # cE->cE

        if "lossy" not in case:
            assert encode_bson(decode_extjson(E)) == cB         # E->cB

            if E != cE:
                assert encode_bson(decode_extjson(cE)) == cB    # cE->cB
```


Most of the tests are converted from the
[General Decimal Arithmetic Testcases](http://speleotrove.com/decimal/dectest.html>).
