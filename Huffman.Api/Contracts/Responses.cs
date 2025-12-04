// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Huffman.Api.Contracts;

public class StatusResponse
{
    public required string Status { get; init; }
    public required DateTime Timestamp { get; init; }
}

public sealed record GreetingResponse
{
    public required string Message { get; init;  }
    public required DateTime Timestamp { get; init; }
}

public sealed record EncodeResponse
{
    public required string Base64Text { get; init; }
    public required int OriginalLength { get; init; }
}

public sealed record DecodeResponse
{
    public required string Text { get; init; }
}