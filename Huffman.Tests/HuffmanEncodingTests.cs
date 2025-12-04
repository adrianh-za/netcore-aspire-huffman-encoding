using Huffman.Core;

namespace Huffman.Tests;

public class HuffmanEncodingTests
{
    [Fact]
    public void Encode_EmptyText_ReturnsEmptyCompressionResult()
    {
        var result = HuffmanCompression.Encode(string.Empty);
        Assert.Empty(result.CompressedData);
        Assert.Empty(result.Codes);
        Assert.Equal(0, result.PaddingBits);
    }

    [Fact]
    public void Encode_NullText_ReturnsEmptyCompressionResult()
    {
        var result = HuffmanCompression.Encode(null!);
        Assert.Empty(result.CompressedData);
        Assert.Empty(result.Codes);
        Assert.Equal(0, result.PaddingBits);
    }

    [Fact]
    public void EncodeDecode_RoundTrip_ReturnsOriginalText()
    {
        const string input = "this is an example for huffman encoding";
        var encoded = HuffmanCompression.Encode(input);

        // basic sanity checks
        Assert.NotNull(encoded.CompressedData);
        Assert.InRange(encoded.PaddingBits, 0, 7);
        Assert.NotEmpty(encoded.Codes);

        var decoded = HuffmanCompression.Decode(encoded.CompressedData, encoded.Codes, encoded.PaddingBits);
        Assert.Equal(input, decoded);
    }

    [Fact]
    public void SingleSymbol_EncodeProducesSingleBitCode_AndDecodeRestores()
    {
        const string  input = "AAAAAAAAAAAAAAAAAAAA";
        var encoded = HuffmanCompression.Encode(input);

        // There should be exactly one code entry for 'A'
        Assert.Single(encoded.Codes);
        Assert.True(encoded.Codes.ContainsKey('A'));
        // Per implementation, single symbol should get at least one bit (encoded as "0")
        Assert.Equal("0", encoded.Codes['A']);

        var decoded = HuffmanCompression.Decode(encoded.CompressedData, encoded.Codes, encoded.PaddingBits);
        Assert.Equal(input, decoded);
    }

    [Fact]
    public void Decode_Throws_WhenCodesDictionaryEmpty()
    {
        var compressed = new byte[] { 0b0000_0000 }; // non-empty compressed buffer
        var emptyCodes = new Dictionary<char, string>();

        //Eppty codes should throw exception
        Assert.Throws<ArgumentException>(() =>
            HuffmanCompression.Decode(compressed, emptyCodes, 0));
    }

    [Fact]
    public void Decode_Throws_OnInvalidPadding()
    {
        var compressed = new byte[] { 0b0000_0000 }; // 8 bits available
        var codes = new Dictionary<char, string> { { 'A', "0" } };

        //Padding greater than available bits should trigger ArgumentException
        Assert.Throws<ArgumentException>(() =>
            HuffmanCompression.Decode(compressed, codes, 9));
    }

    [Fact]
    public void Decode_EmptyCompressedData_ReturnsEmptyString()
    {
        var compressed = Array.Empty<byte>();
        var codes = new Dictionary<char, string>();
        var result = HuffmanCompression.Decode(compressed, codes, 0);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Decode_Throws_OnInvalidCodeCharacters()
    {
        var compressed = new byte[] { 0b0000_0000 };
        var codes = new Dictionary<char, string> { { 'A', "2" } }; // invalid code character

        Assert.Throws<ArgumentException>(() =>
            HuffmanCompression.Decode(compressed, codes, 0));
    }

    [Fact]
    public void Decode_Throws_OnInvalidBitSequence()
    {
        // Tree only contains 'A' mapped to "0" (left). A leading '1' bit will traverse to a null node.
        var compressed = new byte[] { 0b1000_0000 };
        var codes = new Dictionary<char, string> { { 'A', "0" } };

        Assert.Throws<InvalidOperationException>(() =>
            HuffmanCompression.Decode(compressed, codes, 0));
    }

    [Fact]
    public void EncodeDecode_UnicodeCharacters_RoundTrip()
    {
        const string input = "😊 Привет 世界";
        var encoded = HuffmanCompression.Encode(input);

        Assert.NotEmpty(encoded.Codes);
        Assert.InRange(encoded.PaddingBits, 0, 7);

        var decoded = HuffmanCompression.Decode(encoded.CompressedData, encoded.Codes, encoded.PaddingBits);
        Assert.Equal(input, decoded);
    }

    [Fact]
    public void AppendAndExtract_RoundTrip_ReturnsSameCodesAndPayload()
    {
        const string input = "this is an example for append/extract test";
        var encoded = HuffmanCompression.Encode(input);

        var combined = HuffmanCompression.AppendCodesToBytes(encoded.CompressedData, encoded.Codes, encoded.PaddingBits);
        var (codes, padding, payload) = HuffmanCompression.ExtractCodesFromBytes(combined);

        Assert.Equal(encoded.PaddingBits, padding);
        Assert.Equal(encoded.CompressedData, payload);
        Assert.Equal(encoded.Codes.Count, codes.Count);
        foreach (var kv in encoded.Codes)
            Assert.Equal(kv.Value, codes[kv.Key]);

        var decoded = HuffmanCompression.Decode(payload, codes, padding);
        Assert.Equal(input, decoded);
    }

    [Fact]
    public void AppendAndExtract_WithEmptyPayload_ReturnsSameCodesAndEmptyPayload()
    {
        const string input = "payload empty test";
        var encoded = HuffmanCompression.Encode(input);

        // append with empty payload explicitly
        var combined = HuffmanCompression.AppendCodesToBytes(Array.Empty<byte>(), encoded.Codes, encoded.PaddingBits);
        var (codes, padding, payload) = HuffmanCompression.ExtractCodesFromBytes(combined);

        Assert.Empty(payload);
        Assert.Equal(encoded.PaddingBits, padding);
        Assert.Equal(encoded.Codes.Count, codes.Count);
        foreach (var kv in encoded.Codes)
            Assert.Equal(kv.Value, codes[kv.Key]);
    }

    [Fact]
    public void ExtractCodesFromBytes_EmptyData_ReturnsEmpty()
    {
        var (codes, padding, payload) = HuffmanCompression.ExtractCodesFromBytes(Array.Empty<byte>());
        Assert.Empty(codes);
        Assert.Equal(0, padding);
        Assert.Empty(payload);
    }

    [Fact]
    public void ExtractCodesFromBytes_InvalidHeader_Throws()
    {
        const string input = "test invalid header";
        var encoded = HuffmanCompression.Encode(input);
        var combined = HuffmanCompression.AppendCodesToBytes(encoded.CompressedData, encoded.Codes, encoded.PaddingBits);
        // Corrupt header
        combined[0] = 0x00;
        Assert.Throws<ArgumentException>(() => HuffmanCompression.ExtractCodesFromBytes(combined));
    }

    [Fact]
    public void ExtractCodesFromBytes_CorruptedCodesLength_Throws()
    {
        var encoded = HuffmanCompression.Encode("corrupt length");
        var combined = HuffmanCompression.AppendCodesToBytes(encoded.CompressedData, encoded.Codes, encoded.PaddingBits);

        // Overwrite the codes length (bytes 4..7) with a value larger than the array to trigger validation failure
        const int badLength = int.MaxValue;
        var lenBytes = BitConverter.GetBytes(badLength);
        Array.Copy(lenBytes, 0, combined, 4, 4);

        Assert.Throws<ArgumentException>(() => HuffmanCompression.ExtractCodesFromBytes(combined));
    }

    [Fact]
    public void ExtractCodesFromBytes_TruncatedCodes_Throws()
    {
        var encoded = HuffmanCompression.Encode("truncated codes test");
        var combined = HuffmanCompression.AppendCodesToBytes(encoded.CompressedData, encoded.Codes, encoded.PaddingBits);

        //Declared codes length is at offset 4 (little-endian)
        var declaredLen = BitConverter.ToInt32(combined, 4);

        //Create a truncated array that stops before the full codes bytes are present
        var truncatedLen = 4 /*header*/ + 4 /*codesLen*/ + 1 /*padding*/ + Math.Max(0, declaredLen - 1);
        var truncated = combined.Take(truncatedLen).ToArray();

        Assert.Throws<ArgumentException>(() => HuffmanCompression.ExtractCodesFromBytes(truncated));
    }
}