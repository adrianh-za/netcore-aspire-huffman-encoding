// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Huffman.Api.Contracts;

public sealed record EncodeRequest
{
    public required string Text { get; init; }
}

public sealed record DecodeRequest
{
    public required string Base64Text { get; init; }
}