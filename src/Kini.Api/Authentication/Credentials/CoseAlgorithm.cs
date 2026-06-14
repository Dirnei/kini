using System.Formats.Cbor;

namespace Kini.Api.Authentication.Credentials;

/// <summary>
/// Pull the COSE algorithm identifier out of a WebAuthn credential's
/// CBOR-encoded public key blob. COSE_Key is a CBOR map; the algorithm
/// lives at integer label 3. Common values: -7 (ES256), -8 (EdDSA),
/// -257 (RS256). Returns 0 if the blob can't be parsed.
/// </summary>
public static class CoseAlgorithm
{
    public static int Extract(byte[] coseKey)
    {
        if (coseKey is null || coseKey.Length == 0) return 0;
        try
        {
            var reader = new CborReader(coseKey, CborConformanceMode.Strict);
            if (reader.PeekState() != CborReaderState.StartMap) return 0;
            var entries = reader.ReadStartMap() ?? -1;
            // Iterate explicit entry count, OR until the map ends if indefinite.
            for (int i = 0; entries < 0 || i < entries; i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap) break;

                // Read key as a signed integer. Non-integer keys aren't valid
                // for a COSE_Key map but skip just in case.
                if (reader.PeekState() is CborReaderState.UnsignedInteger
                                       or CborReaderState.NegativeInteger)
                {
                    var label = reader.ReadInt64();
                    if (label == 3)
                    {
                        if (reader.PeekState() is CborReaderState.UnsignedInteger
                                              or CborReaderState.NegativeInteger)
                        {
                            return (int)reader.ReadInt64();
                        }
                        return 0;
                    }
                    reader.SkipValue();
                }
                else
                {
                    reader.SkipValue();
                    reader.SkipValue();
                }
            }
        }
        catch
        {
            // Malformed CBOR — caller already stored the blob; we just couldn't
            // surface a human-readable algorithm. Not fatal.
        }
        return 0;
    }
}
