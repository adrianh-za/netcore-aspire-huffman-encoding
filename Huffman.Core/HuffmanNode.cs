namespace Huffman.Core;

/// <summary>
/// Represents a node in a Huffman tree, which is used for encoding and decoding
/// in Huffman compression algorithms. Each node contains a character, its frequency
/// in the input data, and references to its left and right child nodes.
/// </summary>
public sealed record HuffmanNode
{
    public char? Symbol { get; set; } //The character this node represents; null for internal nodes
    public int Frequency { get; set; } //The frequency of the character in the input data
    public HuffmanNode? Left { get; set; } //Reference to the left child node (typically represents '0' in the binary encoding)
    public HuffmanNode? Right { get; set; } //Reference to the right child node (typically represents '1' in the binary encoding)
}