using Huffman.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapStatusEndpoints();
app.MapHuffmanEndpoints();
app.MapOpenApi();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/openapi/v1.json", "Huffman API V1");
});

app.UseHttpsRedirection();

app.Run();