using System.Text;

namespace Huffman.Core;

/// <summary>
/// Provides methods for compressing and uncompressing text data using Huffman encoding, which is a lossless data compression algorithm.
/// </summary>
public static class HuffmanCompression
{
    public record CompressionResult(byte[] CompressedData, Dictionary<char, string> Codes, int PaddingBits);

    /// <summary>
    /// Compresses the specified text using Huffman coding and returns the encoded result.
    /// </summary>
    /// <param name="text">
    /// The input string to compress. If the string is null or empty, the method returns
    /// an empty <see cref="CompressionResult"/>.
    /// </param>
    /// <returns>
    /// A <see cref="CompressionResult"/> containing the compressed byte array.
    /// </returns>
    public static CompressionResult Encode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new CompressionResult(Array.Empty<byte>(), new Dictionary<char, string>(), 0);

        var freq = BuildFrequencyMap(text);
        var rootNode = BuildTree(freq);
        var codes = new Dictionary<char, string>();
        BuildHuffmanCodes(rootNode, "", codes);

        var bitString = EncodeText(text, codes);
        var (bytes, padding) = PackBits(bitString);

        return new CompressionResult(bytes, codes, padding);
    }

    /// <summary>
    /// Builds a frequency map of characters contained in the specified text.
    /// </summary>
    /// <param name="text">
    /// The input string to analyse. Must not be null or empty.
    /// </param>
    /// <returns>
    /// A dictionary where each key is a character from the input string and the
    /// corresponding value is the number of times that character appears.
    /// </returns>
    private static Dictionary<char, int> BuildFrequencyMap(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);

        var charCount = new Dictionary<char, int>(text.Length); //Set max length to avoid resizing
        foreach (var ch in text)
        {
            if (!charCount.TryAdd(ch, 1))
                charCount[ch]++;
        }

        return charCount;
    }

    /// <summary>
    /// Builds a Huffman coding tree from the specified frequency map.
    /// </summary>
    /// <param name="frequencyMap">
    /// A dictionary where each key is a character and the corresponding value is the
    /// frequency (occurrence count) of that character in the input text.
    /// </param>
    /// <returns>
    /// The root <see cref="HuffmanNode"/> of the constructed Huffman tree.
    /// This node represents the full binary tree used for encoding and decoding.
    /// </returns>
    private static HuffmanNode BuildTree(Dictionary<char, int> frequencyMap)
    {
        //Create a list of nodes from frequency map (one node per symbol)
        var queue = new PriorityQueue<HuffmanNode, int>();
        foreach (var (symbol, frequency) in frequencyMap)
        {
            queue.Enqueue(new HuffmanNode
            {
                Symbol = symbol,
                Frequency = frequency
            }, frequency);
        }

        //Edge case: only one distinct symbol => create dummy sibling
        if (queue.Count == 1)
        {
            queue.TryDequeue(out var only, out _);
            var dummy = new HuffmanNode { Symbol = null, Frequency = 0 };

            return new HuffmanNode
            {
                Symbol = null,
                Frequency = only!.Frequency + dummy.Frequency,
                Left = only,
                Right = dummy
            };
        }

        //Build tree the tree.  Will handle both even and odd number of nodes correctly
        //Stops when only one node remains (the root)
        while (queue.Count > 1)
        {
            queue.TryDequeue(out var left, out var leftFreq);
            queue.TryDequeue(out var right, out var rightFreq);

            //Create a new internal node to hold the sum
            var merged = new HuffmanNode
            {
                Symbol = null,
                Frequency = leftFreq + rightFreq,
                Left = left,
                Right = right
            };
            queue.Enqueue(merged, merged.Frequency);
        }

        queue.TryDequeue(out var root, out _);
        return root!;
    }

    /// <summary>
    /// Recursively traverses the Huffman tree to generate binary codes for each character.
    /// </summary>
    /// <param name="node">
    /// The current <see cref="HuffmanNode"/> in the tree.
    /// </param>
    /// <param name="prefix">
    /// The binary string prefix representing the path to the current node.
    /// 0 == left, 1 == right
    /// </param>
    /// <param name="codes">
    /// A dictionary to store the mapping of each character to its Huffman code.
    /// </param>
    private static void BuildHuffmanCodes(HuffmanNode node, string prefix, Dictionary<char, string> codes)
    {
        //Handle LEAF nodes
        if (node.Symbol.HasValue)
        {
            codes[node.Symbol.Value] = prefix.Length > 0 ? prefix : "0"; // ensure at least one bit
            return;
        }

        //Handle INTERNAL nodes - only recurse when children exist
        if (node.Left is not null)
            BuildHuffmanCodes(node.Left, prefix + "0", codes);
        if (node.Right is not null)
            BuildHuffmanCodes(node.Right, prefix + "1", codes);
    }

    /// <summary>
    /// Encodes the input text into a sequence of bits using the provided Huffman codes.
    /// e.g., "0110101..."
    /// </summary>
    /// <param name="text">
    /// The input string to encode.
    /// </param>
    /// <param name="codes">
    /// A dictionary mapping each character to its corresponding Huffman code.
    /// </param>
    /// <returns>
    /// A <see cref="StringBuilder"/> containing the encoded bit string representation of the input text.
    /// </returns>
    private static StringBuilder EncodeText(string text, Dictionary<char, string> codes)
    {
        //Preallocate StringBuilder for efficiency (estimate 4 bits per character)
        var sb = new StringBuilder(text.Length * 4);

        //For each character in the input, append its Huffman code to the result
        foreach (var ch in text)
        {
            sb.Append(codes[ch]);
        }

        return sb;
    }

    /// <summary>
    /// Packs a string of bits into a byte array, adding padding bits if necessary to align to full bytes.
    /// </summary>
    /// <param name="bitString">
    /// A <see cref="StringBuilder"/> containing a sequence of '0' and '1' characters representing bits to pack.
    /// </param>
    /// <returns>
    /// A tuple containing the packed byte array and the number of padding bits added (0-7).
    /// </returns>
    private static (byte[] bytes, int padding) PackBits(StringBuilder bitString)
    {
        //Count the total number of bits in the string
        var bitCount = bitString.Length;

        //Determine the number of '0' bits needed to pad the bit string so its length is a multiple of 8.
        //Padding ensures the bit string can be split into complete bytes for packing. (later in method)
        var padding = (8 - bitCount % 8) % 8;
        if (padding > 0)
        {
            // Append '0's to pad the bit string to a full byte
            bitString.Append('0', padding);
        }

        //Calculate the number of bytes needed
        var byteCount = bitString.Length / 8;
        var result = new byte[byteCount];

        //Convert each group of 8 bits into a byte
        for (var i = 0; i < byteCount; i++)
        {
            byte b = 0;
            for (var bit = 0; bit < 8; bit++)
            {
                //Set the corresponding bit if the character is '1'
                if (bitString[i * 8 + bit] == '1')
                    b |= (byte)(1 << (7 - bit));
            }

            result[i] = b;
        }

        //Return the packed bytes and the number of padding bits added
        return (result, padding);
    }

    /// <summary>
    /// Decodes Huffman-compressed data using the provided Huffman codes and padding information.
    /// </summary>
    /// <param name="compressedData">The compressed byte array.</param>
    /// <param name="codes">Mapping from character to Huffman code (bit string).</param>
    /// <param name="padding">Number of padding bits added to the final byte (0-7).</param>
    /// <returns>The decoded original string.</returns>
    public static string Decode(byte[] compressedData, Dictionary<char, string> codes, int padding)
    {
        if (compressedData.Length == 0)
            return string.Empty;

        if (codes.Count == 0)
            throw new ArgumentException("Codes dictionary must be provided for decoding.", nameof(codes));

        //Reconstruct decoding tree from codes to decode efficiently bit-by-bit.
        var root = new HuffmanNode();
        foreach (var (symbol, value) in codes)
        {
            var code = value ?? throw new ArgumentException("Code strings must not be null.", nameof(codes));
            var node = root;
            foreach (var ch in code)
            {
                // Traverse tree according to each bit in the code
                switch (ch)
                {
                    case '0':
                    {
                        node.Left ??= new HuffmanNode { Symbol = null };
                        node = node.Left;
                        break;
                    }
                    case '1':
                    {
                        node.Right ??= new HuffmanNode { Symbol = null };
                        node = node.Right;
                        break;
                    }
                    default:
                        throw new ArgumentException("Codes must consist only of '0' and '1' characters.", nameof(codes));
                }
            }

            //Assign symbol to leaf node
            node.Symbol = symbol;
        }

        //Calculate total number of bits to decode (excluding padding)
        var totalBits = compressedData.Length * 8 - padding;
        if (totalBits < 0) throw new ArgumentException("Invalid padding value.", nameof(padding));

        var output = new StringBuilder();
        var current = root;

        //Decode each bit and traverse the tree
        for (var bitIndexGlobal = 0; bitIndexGlobal < totalBits; bitIndexGlobal++)
        {
            var byteIndex = bitIndexGlobal / 8;
            var bitIndex = bitIndexGlobal % 8;
            var isOne = (compressedData[byteIndex] & (1 << (7 - bitIndex))) != 0;

            // Move left or right in the tree based on bit value
            current = isOne ? current.Right! : current.Left!;

            if (current is null)
                throw new InvalidOperationException("Encountered an invalid bit sequence that does not map to any symbol.");

            // If at a leaf node, append symbol and reset to root
            if (current.Symbol.HasValue)
            {
                output.Append(current.Symbol.Value);
                current = root;
            }
        }

        return output.ToString();
    }

    /// <summary>
    /// Appends serialized Huffman codes and padding metadata to the provided compressed payload.
    /// The output format is: [4 bytes "HUF1"][int32 codesLength][byte padding][codes bytes][compressed payload bytes].
    /// </summary>
    /// <param name="compressedPayload">The compressed data bytes.</param>
    /// <param name="codes">Dictionary mapping each character to its Huffman code.</param>
    /// <param name="padding">Number of padding bits added to the last byte (0-7).</param>
    /// <returns>
    /// A byte array containing the header, codes metadata, and compressed payload.
    /// </returns>
    public static byte[] AppendCodesToBytes(byte[] compressedPayload, Dictionary<char, string> codes, int padding)
    {
        ArgumentNullException.ThrowIfNull(codes);

        // Serialize the codes dictionary to bytes
        var codesBytes = SerializeCodes(codes);

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        // Write the 4-byte magic header
        bw.Write("HUF1"u8.ToArray());
        // Write the length of the codes section (int32)
        bw.Write(codesBytes.Length);
        // Write the padding value (byte)
        bw.Write((byte)padding);
        // Write the codes bytes
        bw.Write(codesBytes);
        // Write the compressed payload bytes
        bw.Write(compressedPayload);
        bw.Flush();

        // Return the combined byte array
        return ms.ToArray();
    }


    /// <summary>
    /// Extracts the Huffman codes dictionary, padding value, and compressed payload from a combined byte array.
    /// The expected format is [4 bytes header][int32 codesLength][byte padding][codes bytes][compressed payload bytes].
    /// </summary>
    /// <param name="data">The combined byte array containing header, codes, padding, and compressed payload.</param>
    /// <returns>
    /// A tuple containing:
    /// - <see cref="Dictionary{char, string}"/>: The Huffman codes dictionary.
    /// - <see cref="int"/>: The number of padding bits.
    /// - Array of <see cref="byte"/>: The compressed payload.
    /// </returns>
    public static (Dictionary<char, string> Codes, int Padding, byte[] CompressedPayload) ExtractCodesFromBytes(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Handle empty input
        if (data.Length == 0)
            return (new Dictionary<char, string>(), 0, Array.Empty<byte>());

        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

        // Read and validate the 4-byte header
        var header = br.ReadBytes(4);
        if (header is not [(byte)'H', (byte)'U', (byte)'F', (byte)'1'])
            throw new ArgumentException("Data does not contain expected Huffman header.", nameof(data));

        // Read the length of the codes section (int32)
        var codesLen = br.ReadInt32();
        if (codesLen < 0 || codesLen > ms.Length)
            throw new ArgumentException("Invalid codes length in payload.", nameof(data));

        // Read the padding value (byte)
        var padding = br.ReadByte();

        // Read the codes bytes
        var codesBytes = br.ReadBytes(codesLen);
        if (codesBytes.Length != codesLen)
            throw new ArgumentException("Unexpected end of data while reading codes.", nameof(data));

        // Read the remaining bytes as the compressed payload
        var compressedPayload = br.ReadBytes((int)(ms.Length - ms.Position));

        // Deserialize the codes dictionary from the codes bytes
        var codes = DeserializeCodes(codesBytes);

        return (codes, padding, compressedPayload);
    }


    //Helper: serialize codes dictionary to bytes
    private static byte[] SerializeCodes(Dictionary<char, string> codes)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        bw.Write(codes.Count);
        foreach (var (symbol, code) in codes)
        {
            bw.Write(symbol); // char (2 bytes)
            bw.Write(code); // length-prefixed string
        }

        bw.Flush();
        return ms.ToArray();
    }

    //Helper: deserialize codes previously written by SerializeCodes
    private static Dictionary<char, string> DeserializeCodes(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var br = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

        var count = br.ReadInt32();
        var dict = new Dictionary<char, string>(count);
        for (var i = 0; i < count; i++)
        {
            var ch = br.ReadChar();
            var code = br.ReadString();
            dict[ch] = code;
        }

        return dict;
    }
}