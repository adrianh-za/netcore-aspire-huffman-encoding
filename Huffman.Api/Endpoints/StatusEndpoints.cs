using Huffman.Api.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Huffman.Api.Endpoints;

public static class StatusEndpoints
{
    public static void MapStatusEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/status");

        group.MapGet("/greeting", GetGreeting)
            .WithName("GetGreeting")
            .WithSummary("Get greeting message");

        group.MapGet("", GetStatus)
            .WithName("GetStatus")
            .WithSummary("Get API status");
    }

    private static Ok<GreetingResponse> GetGreeting()
    {
        var payload = new GreetingResponse
        {
            Message = "Hello! Welcome to the Huffman API.",
            Timestamp = DateTime.UtcNow
        };
        return TypedResults.Ok(payload);
    }

    private static Ok<StatusResponse> GetStatus()
    {
        var payload = new StatusResponse
        {
            Status = "Success",
            Timestamp = DateTime.UtcNow
        };
        return TypedResults.Ok(payload);
    }
}