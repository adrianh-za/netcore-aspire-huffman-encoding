var builder = DistributedApplication.CreateBuilder(args);

var api = builder
    .AddProject<Projects.Huffman_Api>("huffman-api");

//Add the Swagger endpoint to the service URLs in the Aspire
api.WithUrlForEndpoint("http", _ =>
    new ResourceUrlAnnotation
    {
        Url = "/swagger/index.html",
    }
);

builder.Build().Run();