using System.Text;
using Huffman.Api.Contracts;
using Huffman.Core;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Huffman.Api.Endpoints;

public static class HuffmanEndpoints
{
    public static void MapHuffmanEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/huffman");

        group.MapPost("/encode", Encode)
            .WithName("EncodeString")
            .WithSummary("Encode string with Huffman compression")
            .WithDescription("Encodes the provided string using Huffman compression.  " +
                             "Returns the compressed data in Base64 format along with the original string length." +
                             "The compressed data includes the necessary Huffman codes for decoding.\n\n\n" +
                             "NOTE: Ensure that the specified text is escaped correctly when sending special characters.");

        group.MapPost("/decode", Decode)
            .WithName("DecodeString")
            .WithSummary("Decode Huffman compressed string")
            .WithDescription("Decodes the provided Base64-encoded Huffman compressed string and " +
                             "returns the original uncompressed string." +
                             "The input must include the Huffman codes used during encoding for accurate decoding.");

        group.MapPost("/encode-base64", EncodeBase64)
            .WithName("EncodeBase64String")
            .WithSummary("Encode a base64 string with Huffman compression")
            .WithDescription("Encodes the provided base64 string using Huffman compression.  " +
                             "Returns the compressed data in Base64 format along with the original string length." +
                             "The compressed data includes the necessary Huffman codes for decoding.");
    }

    private static Ok<EncodeResponse> Encode(EncodeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Text);

        var compressionResult = HuffmanCompression.Encode(request.Text);
        var bytes = HuffmanCompression.AppendCodesToBytes(
            compressionResult.CompressedData,
            compressionResult.Codes,
            compressionResult.PaddingBits);

        var payload = new EncodeResponse
        {
            Base64Text = Convert.ToBase64String(bytes),
            OriginalLength = request.Text.Length,
            EncodedLength = bytes.Length
        };

        return TypedResults.Ok(payload);
    }

    private static Ok<EncodeResponse> EncodeBase64(EncodeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Text);

        var base64Bytes = Convert.FromBase64String(request.Text);
        var decodedBase64Text = Encoding.UTF8.GetString(base64Bytes);

        var compressionResult = HuffmanCompression.Encode(decodedBase64Text);
        var bytes = HuffmanCompression.AppendCodesToBytes(
            compressionResult.CompressedData,
            compressionResult.Codes,
            compressionResult.PaddingBits);

        var payload = new EncodeResponse
        {
            Base64Text = Convert.ToBase64String(bytes),
            OriginalLength = request.Text.Length,
            EncodedLength = bytes.Length
        };

        return TypedResults.Ok(payload);
    }

    private static Ok<DecodeResponse> Decode(DecodeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Base64Text);

        var bytes = Convert.FromBase64String(request.Base64Text);
        var (codes, padding, compressedPayload) = HuffmanCompression.ExtractCodesFromBytes(bytes);
        var text = HuffmanCompression.Decode(compressedPayload, codes, padding);

        var payload = new DecodeResponse
        {
            Text = text
        };

        return TypedResults.Ok(payload);
    }
}